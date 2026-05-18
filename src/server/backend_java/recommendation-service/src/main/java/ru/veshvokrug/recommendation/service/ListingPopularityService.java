package ru.veshvokrug.recommendation.service;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.stream.Collectors;

/**
 * Сервис для хранения и изменения популярности объявлений в Redis.
 * Данные хранятся в sorted set: {@code pop:{categorySlug}}.
 *
 * @author Dmitrii Marchenko
 */
@Service
public class ListingPopularityService {
    private static final Logger logger = LoggerFactory.getLogger(ListingPopularityService.class);
    private static final String POPULARITY_KEY_PREFIX = "pop:";
    private static final String NO_LISTINGS_MESSAGE = "Для категории {} объявления не найдены";

    private final StringRedisTemplate redisTemplate;

    public ListingPopularityService(StringRedisTemplate redisTemplate) {
        this.redisTemplate = redisTemplate;
    }

    /**
     * Возвращает ключ Redis для популярности объявлений категории.
     */
    private String getPopularityKey(String categorySlug) {
        return POPULARITY_KEY_PREFIX + categorySlug;
    }

    /**
     * Атомарно изменяет популярность объявления в категории.
     * Гарантирует, что значение не уйдёт ниже нуля.
     *
     * @param categorySlug slug категории
     * @param listingId идентификатор объявления
     * @param weight значение для инкремента, может быть отрицательным
     */
    public void incrementListingPopularity(String categorySlug, String listingId, double weight) {
        String key = getPopularityKey(categorySlug);
        try {
            Double newScore = redisTemplate.opsForZSet().incrementScore(key, listingId, weight);
            if (newScore != null && newScore < 0.0) {
                redisTemplate.opsForZSet().add(key, listingId, 0.0);
                newScore = 0.0;
            }
            logger.debug("Обновлена популярность объявления {} в категории {} до {}",
                    listingId, categorySlug, newScore);
        } catch (Exception e) {
            logger.error("Ошибка при инкременте популярности объявления {} в категории {}",
                    listingId, categorySlug, e);
        }
    }

    /**
     * Возвращает топ-M популярных объявлений категории, отсортированных по убыванию.
     */
    public List<String> getTopListings(String categorySlug, int topM) {
        String key = getPopularityKey(categorySlug);
        try {
            Set<String> listings = redisTemplate.opsForZSet()
                    .reverseRange(key, 0, (long) topM - 1);
            if (listings == null || listings.isEmpty()) {
                logger.debug(NO_LISTINGS_MESSAGE, categorySlug);
                return new ArrayList<>();
            }
            List<String> result = new ArrayList<>(listings);
            logger.debug("Получено {} популярных объявлений для категории {}", result.size(), categorySlug);
            return result;
        } catch (Exception e) {
            logger.error("Ошибка при получении топа объявлений для категории {}", categorySlug, e);
            return new ArrayList<>();
        }
    }

    /**
     * Возвращает топ объявлений с их баллами.
     */
    public Map<String, Double> getTopListingsWithScores(String categorySlug, int topM) {
        String key = getPopularityKey(categorySlug);
        try {
            Set<org.springframework.data.redis.core.ZSetOperations.TypedTuple<String>> scoredListings =
                    redisTemplate.opsForZSet().reverseRangeWithScores(key, 0, (long) topM - 1);
            if (scoredListings == null || scoredListings.isEmpty()) {
                logger.debug(NO_LISTINGS_MESSAGE, categorySlug);
                return new HashMap<>();
            }
            return scoredListings.stream()
                    .collect(Collectors.toMap(
                            org.springframework.data.redis.core.ZSetOperations.TypedTuple::getValue,
                            org.springframework.data.redis.core.ZSetOperations.TypedTuple::getScore,
                            (e1, e2) -> e1,
                            LinkedHashMap::new
                    ));
        } catch (Exception e) {
            logger.error("Ошибка при получении топа объявлений с баллами для категории {}", categorySlug, e);
            return new HashMap<>();
        }
    }

    /**
     * Возвращает все объявления категории.
     */
    public Map<String, Double> getAllListings(String categorySlug) {
        String key = getPopularityKey(categorySlug);
        try {
            Set<org.springframework.data.redis.core.ZSetOperations.TypedTuple<String>> allListings =
                    redisTemplate.opsForZSet().reverseRangeWithScores(key, 0, -1);
            if (allListings == null || allListings.isEmpty()) {
                logger.debug(NO_LISTINGS_MESSAGE, categorySlug);
                return new HashMap<>();
            }
            return allListings.stream()
                    .collect(Collectors.toMap(
                            org.springframework.data.redis.core.ZSetOperations.TypedTuple::getValue,
                            org.springframework.data.redis.core.ZSetOperations.TypedTuple::getScore,
                            (e1, e2) -> e1,
                            LinkedHashMap::new
                    ));
        } catch (Exception e) {
            logger.error("Ошибка при получении всех объявлений категории {}", categorySlug, e);
            return new HashMap<>();
        }
    }

    /**
     * Применяет коэффициент затухания ко всем объявлениям категории.
     */
    public void applyDecay(String categorySlug, double decayMultiplier) {
        String key = getPopularityKey(categorySlug);
        try {
            Set<org.springframework.data.redis.core.ZSetOperations.TypedTuple<String>> listings =
                    redisTemplate.opsForZSet().rangeWithScores(key, 0, -1);
            if (listings == null || listings.isEmpty()) {
                return;
            }
            for (var tuple : listings) {
                Double score = tuple.getScore();
                String value = tuple.getValue();
                if (score != null && value != null) {
                    double newScore = Math.max(score * decayMultiplier, 0.0);
                    redisTemplate.opsForZSet().add(key, value, newScore);
                }
            }
            logger.debug("Применён decay {} к объявлениям категории {}", decayMultiplier, categorySlug);
        } catch (Exception e) {
            logger.error("Ошибка при применении decay к категории {}", categorySlug, e);
        }
    }

    /**
     * Возвращает количество объявлений в категории.
     */
    public long getListingCount(String categorySlug) {
        String key = getPopularityKey(categorySlug);
        try {
            Long count = redisTemplate.opsForZSet().size(key);
            return count != null ? count : 0;
        } catch (Exception e) {
            logger.error("Ошибка при получении количества объявлений категории {}", categorySlug, e);
            return 0;
        }
    }
}

