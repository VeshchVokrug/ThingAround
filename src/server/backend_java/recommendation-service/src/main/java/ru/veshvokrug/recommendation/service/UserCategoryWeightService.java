package ru.veshvokrug.recommendation.service;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.stereotype.Service;

import java.util.HashMap;
import java.util.LinkedHashMap;
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
            Double newWeight = redisTemplate.opsForHash().increment(key, categorySlug, weight);
            if (newWeight != null && newWeight < 0.0) {
                redisTemplate.opsForHash().put(key, categorySlug, String.valueOf(0.0));
                newWeight = 0.0;
            }
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
                    .sorted((a, b) -> Double.compare(b.getValue(), a.getValue()))
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
     * Применяет коэффициент затухания ко всем весам категорий пользователя.
     */
    public void applyDecay(String userId, double decayMultiplier) {
        String key = getUserCategoryKey(userId);
        try {
            Map<Object, Object> categories = redisTemplate.opsForHash().entries(key);
            for (Map.Entry<Object, Object> entry : categories.entrySet()) {
                try {
                    double currentWeight = Double.parseDouble(entry.getValue().toString());
                    double newWeight = currentWeight * decayMultiplier;
                    redisTemplate.opsForHash().put(key, entry.getKey().toString(), String.valueOf(newWeight));
                } catch (NumberFormatException e) {
                    logger.warn("Пропускаю некорректный вес для пользователя {} и категории {}", userId, entry.getKey(), e);
                }
            }
            logger.debug("Применён decay {} для пользователя {}", decayMultiplier, userId);
        } catch (Exception e) {
            logger.error("Ошибка при применении decay для пользователя {}", userId, e);
        }
    }

    /**
     * Удаляет категории с весом ниже порога и возвращает количество удалённых полей.
     */
    public int removeWeightsBelowThreshold(String userId, double threshold) {
        String key = getUserCategoryKey(userId);
        int deletedCount = 0;
        try {
            Map<Object, Object> categories = redisTemplate.opsForHash().entries(key);
            for (Map.Entry<Object, Object> entry : categories.entrySet()) {
                try {
                    double weight = Double.parseDouble(entry.getValue().toString());
                    if (weight < threshold) {
                        redisTemplate.opsForHash().delete(key, entry.getKey());
                        deletedCount++;
                    }
                } catch (NumberFormatException e) {
                    logger.warn("Пропускаю некорректный вес при очистке: пользователь {} категория {}", userId, entry.getKey());
                }
            }
            logger.debug("Удалено {} весов категорий ниже порога {} для пользователя {}", deletedCount, threshold, userId);
        } catch (Exception e) {
            logger.error("Ошибка при удалении весов ниже порога для пользователя {}", userId, e);
        }
        return deletedCount;
    }
}

