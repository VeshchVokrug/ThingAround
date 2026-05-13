package ru.veshvokrug.recommendation.config;

import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.stereotype.Component;

/**
 * Конфигурация параметров весов и алгоритма рекомендаций.
 *
 * @author Dmitrii Marchenko
 */
@Component
@ConfigurationProperties(prefix = "recommendation.weights")
public class EventWeightsConfig {

    private int userInterestHalfLifeDays = 30;
    private int listingPopularityHalfLifeDays = 14;
    private double minCategoryWeightThreshold = 0.001;
    private int topCategoriesCount = 5;
    private int topListingsPerCategory = -1;
    private int defaultRecommendationSize = 20;
    private long recommendationCacheTtlSeconds = 300;

    public int getUserInterestHalfLifeDays() {
        return userInterestHalfLifeDays;
    }

    public void setUserInterestHalfLifeDays(int userInterestHalfLifeDays) {
        this.userInterestHalfLifeDays = userInterestHalfLifeDays;
    }

    public int getListingPopularityHalfLifeDays() {
        return listingPopularityHalfLifeDays;
    }

    public void setListingPopularityHalfLifeDays(int listingPopularityHalfLifeDays) {
        this.listingPopularityHalfLifeDays = listingPopularityHalfLifeDays;
    }

    public double getMinCategoryWeightThreshold() {
        return minCategoryWeightThreshold;
    }

    public void setMinCategoryWeightThreshold(double minCategoryWeightThreshold) {
        this.minCategoryWeightThreshold = minCategoryWeightThreshold;
    }

    public int getTopCategoriesCount() {
        return topCategoriesCount;
    }

    public void setTopCategoriesCount(int topCategoriesCount) {
        this.topCategoriesCount = topCategoriesCount;
    }

    public int getTopListingsPerCategory() {
        if (topListingsPerCategory > 0) {
            return topListingsPerCategory;
        }
        return resolveTopListingsPerCategory(defaultRecommendationSize, topCategoriesCount);
    }

    /**
     * Вычисляет количество объявлений на одну категорию для конкретного размера выдачи.
     * Если параметр явно задан в конфигурации, используется он.
     */
    public int resolveTopListingsPerCategory(int recommendationSize, int categoryCount) {
        if (topListingsPerCategory > 0) {
            return topListingsPerCategory;
        }
        int safeSize = recommendationSize > 0 ? recommendationSize : defaultRecommendationSize;
        int safeCategoryCount = categoryCount > 0 ? categoryCount : Math.max(topCategoriesCount, 1);
        return (int) Math.ceil((double) safeSize / safeCategoryCount);
    }

    public void setTopListingsPerCategory(int topListingsPerCategory) {
        this.topListingsPerCategory = topListingsPerCategory;
    }

    public int getDefaultRecommendationSize() {
        return defaultRecommendationSize;
    }

    public void setDefaultRecommendationSize(int defaultRecommendationSize) {
        this.defaultRecommendationSize = defaultRecommendationSize;
    }

    public long getRecommendationCacheTtlSeconds() {
        return recommendationCacheTtlSeconds;
    }

    public void setRecommendationCacheTtlSeconds(long recommendationCacheTtlSeconds) {
        this.recommendationCacheTtlSeconds = recommendationCacheTtlSeconds;
    }
}

