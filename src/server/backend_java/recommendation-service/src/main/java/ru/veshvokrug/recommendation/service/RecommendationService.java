package ru.veshvokrug.recommendation.service;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.stereotype.Service;
import ru.veshvokrug.recommendation.config.EventWeightsConfig;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;

import java.util.ArrayList;
import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.TimeUnit;

/**
 * Сервис для формирования персональных рекомендаций.
 * Объединяет веса интересов пользователя и популярность объявлений.
 *
 * @author Dmitrii Marchenko
 */
@Service
public class RecommendationService {
    private static final Logger logger = LoggerFactory.getLogger(RecommendationService.class);
    private static final String CACHE_KEY_PREFIX = "rec:";

    private final UserCategoryWeightService userCategoryWeightService;
    private final ListingPopularityService listingPopularityService;
    private final EventWeightsConfig weightsConfig;
    private final StringRedisTemplate redisTemplate;
    private final ObjectMapper objectMapper;

    public RecommendationService(
            UserCategoryWeightService userCategoryWeightService,
            ListingPopularityService listingPopularityService,
            EventWeightsConfig weightsConfig,
            StringRedisTemplate redisTemplate,
            ObjectMapper objectMapper) {
        this.userCategoryWeightService = userCategoryWeightService;
        this.listingPopularityService = listingPopularityService;
        this.weightsConfig = weightsConfig;
        this.redisTemplate = redisTemplate;
        this.objectMapper = objectMapper;
    }

    /**
     * Формирует рекомендации для пользователя.
     *
     * @param userId идентификатор пользователя
     * @param size количество рекомендаций
     * @return список рекомендованных идентификаторов объявлений
     */
    public List<String> getRecommendations(String userId, int size) {
        if (userId == null || userId.isBlank()) {
            logger.warn("Передан некорректный userId для рекомендаций");
            return Collections.emptyList();
        }

        int actualSize = size > 0 ? size : weightsConfig.getDefaultRecommendationSize();
        String cacheKey = buildCacheKey(userId, actualSize);

        List<String> cached = readFromCache(cacheKey);
        if (cached != null) {
            logger.debug("Возвращаю рекомендации пользователя {} из кэша", userId);
            return cached;
        }

        try {
            // Шаг 1: получаем топ-K категорий пользователя
            int topK = weightsConfig.getTopCategoriesCount();
            Map<String, Double> topCategories = userCategoryWeightService.getTopCategories(userId, topK);

            if (topCategories.isEmpty()) {
                logger.debug("Для пользователя {} не найдено категорий", userId);
                return Collections.emptyList();
            }

            logger.debug("Найдено {} основных категорий для пользователя {}", topCategories.size(), userId);

            // Шаг 2: для каждой категории берём топ популярных объявлений
            int listingsPerCategory = weightsConfig.resolveTopListingsPerCategory(actualSize, topCategories.size());
            Map<String, List<String>> listingsByCategory = new LinkedHashMap<>();

            for (String categorySlug : topCategories.keySet()) {
                List<String> categoryListings = listingPopularityService
                        .getTopListings(categorySlug, listingsPerCategory);
                if (!categoryListings.isEmpty()) {
                    listingsByCategory.put(categorySlug, categoryListings);
                }
                logger.debug("Получено {} объявлений из категории {} для пользователя {}",
                        categoryListings.size(), categorySlug, userId);
            }

            if (listingsByCategory.isEmpty()) {
                logger.debug("Не найдено объявлений для пользователя {} среди основных категорий", userId);
                return Collections.emptyList();
            }

            // Шаг 3: собираем результат round-robin, чтобы категории чередовались.
            List<String> result = collectRoundRobin(listingsByCategory, actualSize);

            writeToCache(cacheKey, result);
            logger.debug("Сформировано {} рекомендаций для пользователя {}", result.size(), userId);
            return result;

        } catch (Exception e) {
            logger.error("Ошибка при формировании рекомендаций для пользователя {}", userId, e);
            return Collections.emptyList();
        }
    }

    /**
     * Возвращает размер выдачи по умолчанию.
     */
    public int getDefaultSize() {
        return weightsConfig.getDefaultRecommendationSize();
    }

    private String buildCacheKey(String userId, int size) {
        return CACHE_KEY_PREFIX + userId + ":" + size;
    }

    private List<String> readFromCache(String cacheKey) {
        try {
            String payload = redisTemplate.opsForValue().get(cacheKey);
            if (payload == null || payload.isBlank()) {
                return null;
            }
            return objectMapper.readValue(payload, new TypeReference<List<String>>() {});
        } catch (Exception e) {
            logger.warn("Не удалось прочитать кэш рекомендаций {}", cacheKey, e);
            return null;
        }
    }

    private void writeToCache(String cacheKey, List<String> recommendations) {
        try {
            String payload = objectMapper.writeValueAsString(recommendations);
            redisTemplate.opsForValue().set(
                    cacheKey,
                    payload,
                    weightsConfig.getRecommendationCacheTtlSeconds(),
                    TimeUnit.SECONDS);
        } catch (Exception e) {
            logger.warn("Не удалось записать кэш рекомендаций {}", cacheKey, e);
        }
    }

    private List<String> collectRoundRobin(Map<String, List<String>> listingsByCategory, int limit) {
        List<String> result = new ArrayList<>(limit);
        Map<String, Integer> indexes = new LinkedHashMap<>();
        listingsByCategory.keySet().forEach(category -> indexes.put(category, 0));

        while (result.size() < limit) {
            boolean addedInPass = false;
            for (Map.Entry<String, List<String>> entry : listingsByCategory.entrySet()) {
                String category = entry.getKey();
                List<String> listings = entry.getValue();
                int index = indexes.get(category);
                if (index >= listings.size()) {
                    continue;
                }

                String listingId = listings.get(index);
                indexes.put(category, index + 1);
                if (!result.contains(listingId)) {
                    result.add(listingId);
                    addedInPass = true;
                    if (result.size() >= limit) {
                        break;
                    }
                }
            }
            if (!addedInPass) {
                break;
            }
        }
        return result;
    }
}

