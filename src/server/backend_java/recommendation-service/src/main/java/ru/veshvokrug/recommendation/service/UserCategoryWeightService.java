package ru.veshvokrug.recommendation.service;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.data.redis.core.script.DefaultRedisScript;
import org.springframework.data.redis.core.script.RedisScript;
import org.springframework.stereotype.Service;

import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

/**
 * Сервис для хранения и изменения весов интереса пользователя к категориям в Redis.
 * Данные хранятся в hash-структуре: {@code user:{userId}:cat_weights}.
 *
 * @author Dmitrii Marchenko
 */
@Service
public class UserCategoryWeightService {
    private static final Logger logger = LoggerFactory.getLogger(UserCategoryWeightService.class);
    private static final String USER_CATEGORY_KEY_PREFIX = "user:";
    private static final String USER_CATEGORY_KEY_SUFFIX = ":cat_weights";

    /**
     * Атомарный инкремент с нижней границей 0: BookingCancelled даёт
     * отрицательные веса, и без атомарности clamp терял бы конкурентные
     * инкременты между HINCRBYFLOAT и записью нуля.
     */
    private static final RedisScript<String> INCREMENT_CLAMPED_SCRIPT = new DefaultRedisScript<>("""
            local value = redis.call('HINCRBYFLOAT', KEYS[1], ARGV[1], ARGV[2])
            if tonumber(value) < 0 then
              redis.call('HSET', KEYS[1], ARGV[1], '0')
              return '0'
            end
            return value
            """, String.class);

    /**
     * Атомарное затухание с одновременной чисткой малых весов.
     * Один скрипт вместо HGETALL + посчитать + перезаписать исключает
     * потерю инкрементов, пришедших во время пересчёта.
     */
    private static final RedisScript<Long> DECAY_AND_CLEANUP_SCRIPT = new DefaultRedisScript<>("""
            local entries = redis.call('HGETALL', KEYS[1])
            local multiplier = tonumber(ARGV[1])
            local threshold = tonumber(ARGV[2])
            local removed = 0
            for i = 1, #entries, 2 do
              local value = tonumber(entries[i + 1])
              if value then
                local updated = value * multiplier
                if updated < threshold then
                  redis.call('HDEL', KEYS[1], entries[i])
                  removed = removed + 1
                else
                  redis.call('HSET', KEYS[1], entries[i], tostring(updated))
                end
              end
            end
            return removed
            """, Long.class);

    private final StringRedisTemplate redisTemplate;

    public UserCategoryWeightService(StringRedisTemplate redisTemplate) {
        this.redisTemplate = redisTemplate;
    }

    /**
     * Возвращает ключ Redis для весов категорий пользователя.
     */
    private String getUserCategoryKey(String userId) {
        return USER_CATEGORY_KEY_PREFIX + userId + USER_CATEGORY_KEY_SUFFIX;
    }

    /**
     * Атомарно изменяет вес интереса пользователя к категории.
     *
     * @param userId идентификатор пользователя
     * @param categorySlug slug категории
     * @param weight значение для инкремента, может быть отрицательным
     */
    public void incrementCategoryWeight(String userId, String categorySlug, double weight) {
        String key = getUserCategoryKey(userId);
        try {
            String newWeight = redisTemplate.execute(
                    INCREMENT_CLAMPED_SCRIPT,
                    List.of(key),
                    categorySlug,
                    String.valueOf(weight));
            logger.debug("Обновлён вес категории {} пользователя {} до {}", categorySlug, userId, newWeight);
        } catch (Exception e) {
            logger.error("Ошибка при инкременте веса категории {} пользователя {}", categorySlug, userId, e);
        }
    }

    /**
     * Возвращает топ-K категорий пользователя, отсортированных по весу по убыванию.
     */
    public Map<String, Double> getTopCategories(String userId, int topK) {
        String key = getUserCategoryKey(userId);
        try {
            Map<Object, Object> allWeights = redisTemplate.opsForHash().entries(key);
            if (allWeights.isEmpty()) {
                logger.debug("Для пользователя {} не найдено категорий", userId);
                return new HashMap<>();
            }

            return allWeights.entrySet().stream()
                    .collect(Collectors.toMap(
                            e -> e.getKey().toString(),
                            e -> {
                                try {
                                    return Double.parseDouble(e.getValue().toString());
                                } catch (NumberFormatException ex) {
                                    return 0.0;
                                }
                            }
                    ))
                    .entrySet().stream()
                    .sorted((a, b) ->
                            Double.compare(b.getValue(), a.getValue()))
                    .limit(topK)
                    .collect(Collectors.toMap(
                            Map.Entry::getKey,
                            Map.Entry::getValue,
                            (e1, e2) -> e1,
                            LinkedHashMap::new
                    ));
        } catch (Exception e) {
            logger.error("Ошибка при получении топа категорий пользователя {}", userId, e);
            return new HashMap<>();
        }
    }

    /**
     * Возвращает все категории пользователя.
     */
    public Map<String, Double> getAllCategories(String userId) {
        String key = getUserCategoryKey(userId);
        try {
            Map<Object, Object> allWeights = redisTemplate.opsForHash().entries(key);
            return allWeights.entrySet().stream()
                    .collect(Collectors.toMap(
                            e -> e.getKey().toString(),
                            e -> {
                                try {
                                    return Double.parseDouble(e.getValue().toString());
                                } catch (NumberFormatException ex) {
                                    return 0.0;
                                }
                            }
                    ));
        } catch (Exception e) {
            logger.error("Ошибка при получении категорий пользователя {}", userId, e);
            return new HashMap<>();
        }
    }

    /**
     * Атомарно применяет затухание ко всем весам категорий пользователя
     * и удаляет веса, опустившиеся ниже порога.
     *
     * @return количество удалённых категорий
     */
    public long applyDecay(String userId, double decayMultiplier, double minWeightThreshold) {
        String key = getUserCategoryKey(userId);
        try {
            Long removed = redisTemplate.execute(
                    DECAY_AND_CLEANUP_SCRIPT,
                    List.of(key),
                    String.valueOf(decayMultiplier),
                    String.valueOf(minWeightThreshold));
            logger.debug("Применён decay {} для пользователя {}, удалено весов: {}",
                    decayMultiplier, userId, removed);
            return removed == null ? 0 : removed;
        } catch (Exception e) {
            logger.error("Ошибка при применении decay для пользователя {}", userId, e);
            return 0;
        }
    }
}
