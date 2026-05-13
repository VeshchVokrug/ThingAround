package ru.veshvokrug.recommendation.config;

import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;

/**
 * Тесты для {@link EventWeightsConfig}.
 */
class EventWeightsConfigTest {

    @Test
    void shouldResolveTopListingsPerCategoryAutomatically() {
        EventWeightsConfig config = new EventWeightsConfig();
        config.setDefaultRecommendationSize(20);
        config.setTopCategoriesCount(5);

        assertEquals(4, config.resolveTopListingsPerCategory(20, 5));
        assertEquals(3, config.resolveTopListingsPerCategory(9, 4));
    }

    @Test
    void shouldUseConfiguredTopListingsPerCategoryAsIs() {
        EventWeightsConfig config = new EventWeightsConfig();
        config.setTopListingsPerCategory(7);

        assertEquals(7, config.resolveTopListingsPerCategory(20, 5));
        assertEquals(7, config.getTopListingsPerCategory());
    }
}

