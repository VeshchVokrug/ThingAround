package ru.veshvokrug.recommendation.service;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.data.redis.core.Cursor;
import org.springframework.data.redis.core.ScanOptions;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.data.redis.core.script.DefaultRedisScript;
import org.springframework.data.redis.core.script.RedisScript;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import ru.veshvokrug.recommendation.config.EventWeightsConfig;

import java.time.Duration;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

/**
 * Планировщик для применения временного затухания весов.
 * Периодически уменьшает влияние устаревших интересов и популярных объявлений.
 *
 * Ключи перебираются через SCAN (KEYS блокирует Redis на больших объёмах),
 * а запуск защищён Redis-локом — при нескольких инстансах сервиса затухание
 * применяется ровно один раз.
 *
 * @author Dmitrii Marchenko
 */
@Service
public class WeightDecayScheduler {
    private static final Logger logger = LoggerFactory.getLogger(WeightDecayScheduler.class);
    private static final String USER_CATEGORY_PATTERN = "user:*:cat_weights";
    private static final String POPULARITY_PATTERN = "pop:*";
    private static final String USER_DECAY_LOCK_KEY = "lock:decay:user-categories";
    private static final String POPULARITY_DECAY_LOCK_KEY = "lock:decay:listing-popularity";
    private static final Duration LOCK_TTL = Duration.ofMinutes(30);
    private static final int SCAN_BATCH_SIZE = 500;

    /** Удаляет лок, только если он всё ещё принадлежит этому инстансу. */
    private static final RedisScript<Long> RELEASE_LOCK_SCRIPT = new DefaultRedisScript<>("""
            if redis.call('GET', KEYS[1]) == ARGV[1] then
              return redis.call('DEL', KEYS[1])
            end
            return 0
            """, Long.class);

    private final StringRedisTemplate redisTemplate;
    private final EventWeightsConfig weightsConfig;
    private final UserCategoryWeightService userCategoryWeightService;
    private final ListingPopularityService listingPopularityService;

    public WeightDecayScheduler(
            StringRedisTemplate redisTemplate,
            EventWeightsConfig weightsConfig,
            UserCategoryWeightService userCategoryWeightService,
            ListingPopularityService listingPopularityService) {
        this.redisTemplate = redisTemplate;
        this.weightsConfig = weightsConfig;
        this.userCategoryWeightService = userCategoryWeightService;
        this.listingPopularityService = listingPopularityService;
    }

    /**
     * Применяет затухание ко всем весам категорий пользователей.
     * Запускается ежедневно в 00:00.
     */
    @Scheduled(cron = "0 0 0 * * *")
    public void applyUserCategoryDecay() {
        runWithLock(USER_DECAY_LOCK_KEY, "затухание весов категорий пользователей", () -> {
            double decayMultiplier = calculateDecayMultiplier(
                    weightsConfig.getUserInterestHalfLifeDays());
            if (decayMultiplier >= 1.0) {
                return;
            }
            double threshold = weightsConfig.getMinCategoryWeightThreshold();

            int processedUsers = 0;
            long removedWeights = 0;
            for (String key : scanKeys(USER_CATEGORY_PATTERN)) {
                String userId = extractUserIdFromKey(key);
                removedWeights += userCategoryWeightService.applyDecay(userId, decayMultiplier, threshold);
                processedUsers++;
            }
            logger.info("Затухание категорий завершено: пользователей {}, удалено малых весов {}",
                    processedUsers, removedWeights);
        });
    }

    /**
     * Применяет затухание ко всем значениям популярности объявлений.
     * Запускается ежедневно в 01:00 после затухания пользовательских категорий.
     */
    @Scheduled(cron = "0 0 1 * * *")
    public void applyListingPopularityDecay() {
        runWithLock(POPULARITY_DECAY_LOCK_KEY, "затухание популярности объявлений", () -> {
            double decayMultiplier = calculateDecayMultiplier(
                    weightsConfig.getListingPopularityHalfLifeDays());
            if (decayMultiplier >= 1.0) {
                return;
            }

            int processedCategories = 0;
            for (String key : scanKeys(POPULARITY_PATTERN)) {
                listingPopularityService.applyDecay(extractCategoryFromKey(key), decayMultiplier);
                processedCategories++;
            }
            logger.info("Затухание популярности завершено: категорий {}", processedCategories);
        });
    }

    private void runWithLock(String lockKey, String taskName, Runnable task) {
        String lockOwner = UUID.randomUUID().toString();
        Boolean acquired = redisTemplate.opsForValue().setIfAbsent(lockKey, lockOwner, LOCK_TTL);
        if (!Boolean.TRUE.equals(acquired)) {
            logger.info("Пропускаю задачу '{}': лок {} занят другим инстансом", taskName, lockKey);
            return;
        }

        long startTime = System.currentTimeMillis();
        logger.info("Запускаю задачу: {}", taskName);
        try {
            task.run();
            logger.info("Задача '{}' выполнена за {} мс", taskName, System.currentTimeMillis() - startTime);
        } catch (Exception e) {
            logger.error("Ошибка во время задачи '{}'", taskName, e);
        } finally {
            try {
                redisTemplate.execute(RELEASE_LOCK_SCRIPT, List.of(lockKey), lockOwner);
            } catch (Exception e) {
                logger.warn("Не удалось освободить лок {} (истечёт по TTL)", lockKey, e);
            }
        }
    }

    private List<String> scanKeys(String pattern) {
        List<String> keys = new ArrayList<>();
        ScanOptions options = ScanOptions.scanOptions().match(pattern).count(SCAN_BATCH_SIZE).build();
        try (Cursor<String> cursor = redisTemplate.scan(options)) {
            while (cursor.hasNext()) {
                keys.add(cursor.next());
            }
        } catch (Exception e) {
            logger.error("Ошибка при SCAN по шаблону {}", pattern, e);
        }
        return keys;
    }

    /**
     * Вычисляет экспоненциальный коэффициент затухания.
     * Формула: exp(-lambda * delta_days), где lambda = ln(2) / half_life_days.
     *
     * @param halfLifeDays период полураспада в днях
     * @return коэффициент затухания (0 < multiplier <= 1)
     */
    private double calculateDecayMultiplier(int halfLifeDays) {
        if (halfLifeDays <= 0) {
            logger.warn("halfLifeDays={} некорректен; затухание пропущено (multiplier=1.0)", halfLifeDays);
            return 1.0;
        }

        // Затухание за 1 день
        double lambda = Math.log(2.0) / halfLifeDays;
        double decayMultiplier = Math.exp(-lambda * 1.0);
        logger.debug("Рассчитан коэффициент затухания для полураспада {} дней: {}",
                halfLifeDays, decayMultiplier);
        return decayMultiplier;
    }

    /**
     * Извлекает userId из Redis-ключа формата "user:{userId}:cat_weights".
     */
    private String extractUserIdFromKey(String key) {
        int startIdx = "user:".length();
        int endIdx = key.lastIndexOf(":cat_weights");
        return key.substring(startIdx, endIdx);
    }

    /**
     * Извлекает categorySlug из Redis-ключа формата "pop:{categorySlug}".
     */
    private String extractCategoryFromKey(String key) {
        return key.substring("pop:".length());
    }
}
