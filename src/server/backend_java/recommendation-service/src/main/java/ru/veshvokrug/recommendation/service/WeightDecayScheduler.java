package ru.veshvokrug.recommendation.service;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import ru.veshvokrug.recommendation.config.EventWeightsConfig;

import java.time.Instant;
import java.util.Set;

/**
 * Планировщик для применения временного затухания весов.
 * Периодически уменьшает влияние устаревших интересов и популярных объявлений.
 *
 * @author Dmitrii Marchenko
 */
@Service
public class WeightDecayScheduler {
    private static final Logger logger = LoggerFactory.getLogger(WeightDecayScheduler.class);
    private static final String USER_CATEGORY_PATTERN = "user:*:cat_weights";
    private static final String POPULARITY_PATTERN = "pop:*";

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
        logger.info("Запускаю задачу затухания весов категорий пользователей");
        long startTime = System.currentTimeMillis();

        try {
            double decayMultiplier = calculateDecayMultiplier(
                    weightsConfig.getUserInterestHalfLifeDays());

            Set<String> keys = redisTemplate.keys(USER_CATEGORY_PATTERN);
            if (keys == null || keys.isEmpty()) {
                logger.debug("Нет весов категорий пользователей для затухания");
                return;
            }

            int processedUsers = 0;
            for (String key : keys) {
                try {
                    // Извлекаем userId из ключа формата "user:{userId}:cat_weights"
                    String userId = extractUserIdFromKey(key);
                    userCategoryWeightService.applyDecay(userId, decayMultiplier);

                    // Удаляем очень маленькие веса, чтобы не копился мусор
                    int removed = userCategoryWeightService.removeWeightsBelowThreshold(
                            userId, weightsConfig.getMinCategoryWeightThreshold());

                    if (removed > 0) {
                        logger.debug("Удалено {} маленьких весов у пользователя {}", removed, userId);
                    }
                    processedUsers++;
                } catch (Exception e) {
                    logger.error("Ошибка при обработке затухания категорий для ключа {}", key, e);
                }
            }

            long duration = System.currentTimeMillis() - startTime;
            logger.info("Затухание категорий завершено: обработано пользователей {}, {} мс",
                    processedUsers, duration);

        } catch (Exception e) {
            logger.error("Ошибка во время затухания весов категорий пользователей", e);
        }
    }

    /**
     * Применяет затухание ко всем значениям популярности объявлений.
     * Запускается ежедневно в 01:00 после затухания пользовательских категорий.
     */
    @Scheduled(cron = "0 0 1 * * *")
    public void applyListingPopularityDecay() {
        logger.info("Запускаю задачу затухания популярности объявлений");
        long startTime = System.currentTimeMillis();

        try {
            double decayMultiplier = calculateDecayMultiplier(
                    weightsConfig.getListingPopularityHalfLifeDays());

            Set<String> keys = redisTemplate.keys(POPULARITY_PATTERN);
            if (keys == null || keys.isEmpty()) {
                logger.debug("Нет популярности объявлений для затухания");
                return;
            }

            int processedCategories = 0;
            for (String key : keys) {
                try {
                    // Извлекаем categorySlug из ключа формата "pop:{categorySlug}"
                    String categorySlug = extractCategoryFromKey(key);
                    listingPopularityService.applyDecay(categorySlug, decayMultiplier);
                    processedCategories++;
                } catch (Exception e) {
                    logger.error("Ошибка при обработке затухания популярности для ключа {}", key, e);
                }
            }

            long duration = System.currentTimeMillis() - startTime;
            logger.info("Затухание популярности завершено: обработано категорий {}, {} мс",
                    processedCategories, duration);

        } catch (Exception e) {
            logger.error("Ошибка во время затухания популярности объявлений", e);
        }
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

