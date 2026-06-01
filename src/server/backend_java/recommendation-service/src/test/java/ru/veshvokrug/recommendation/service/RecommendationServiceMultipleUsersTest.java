package ru.veshvokrug.recommendation.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.data.redis.core.ValueOperations;
import ru.veshvokrug.recommendation.config.EventWeightsConfig;

import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.Mockito.lenient;
import static org.mockito.Mockito.when;

/**
 * Тесты, проверяющие, что рекомендации считаются корректно для разных пользователей
 * и что категории без доступных объявлений пропускаются.
 */
@ExtendWith(MockitoExtension.class)
class RecommendationServiceMultipleUsersTest {

    @Mock
    private UserCategoryWeightService userCategoryWeightService;

    @Mock
    private ListingPopularityService listingPopularityService;

    @Mock
    private StringRedisTemplate redisTemplate;

    @Mock
    private ValueOperations<String, String> valueOperations;

    private RecommendationService recommendationService;

    @BeforeEach
    void setUp() {
        EventWeightsConfig config = new EventWeightsConfig();
        config.setTopCategoriesCount(2);
        config.setDefaultRecommendationSize(4);
        config.setRecommendationCacheTtlSeconds(300);

        lenient().when(redisTemplate.opsForValue()).thenReturn(valueOperations);
        recommendationService = new RecommendationService(
                userCategoryWeightService,
                listingPopularityService,
                config,
                redisTemplate,
                new ObjectMapper()
        );
    }

    @Test
    void recommendationsShouldDifferForDifferentUsersBasedOnTopCategoryOrder() {
        Map<String, Double> categoriesA = new LinkedHashMap<>();
        categoriesA.put("sports", 10.0);
        categoriesA.put("tools", 5.0);

        Map<String, Double> categoriesB = new LinkedHashMap<>();
        categoriesB.put("tools", 8.0);
        categoriesB.put("sports", 3.0);

        when(userCategoryWeightService.getTopCategories("userA", 2)).thenReturn(categoriesA);
        when(userCategoryWeightService.getTopCategories("userB", 2)).thenReturn(categoriesB);

        when(listingPopularityService.getTopListings("sports", 2)).thenReturn(List.of("s1", "s2"));
        when(listingPopularityService.getTopListings("tools", 2)).thenReturn(List.of("t1", "t2"));

        List<String> recA = recommendationService.getRecommendations("userA", 4);
        List<String> recB = recommendationService.getRecommendations("userB", 4);

        assertTrue(recA.size() > 0 && recA.get(0).startsWith("s"));

        assertTrue(recB.size() > 0 && recB.get(0).startsWith("t"));

        assertTrue(!recA.equals(recB));
    }

    @Test
    void shouldSkipCategoriesWhichHaveNoListings() {
        Map<String, Double> categories = new LinkedHashMap<>();
        categories.put("emptycat", 9.0);
        categories.put("sports", 5.0);

        when(userCategoryWeightService.getTopCategories("userX", 2)).thenReturn(categories);

        when(listingPopularityService.getTopListings("emptycat", 2)).thenReturn(List.of());
        when(listingPopularityService.getTopListings("sports", 2)).thenReturn(List.of("s1", "s2"));

        List<String> rec = recommendationService.getRecommendations("userX", 3);

        assertEquals(2, rec.size());
        assertTrue(rec.stream().allMatch(id -> id.startsWith("s")));
    }

    @Test
    void shouldReturnEmptyWhenNoCategoriesOrListingsAvailable() {
        when(userCategoryWeightService.getTopCategories("nouser", 2)).thenReturn(Map.of());

        List<String> rec = recommendationService.getRecommendations("nouser", 5);

        assertTrue(rec.isEmpty());
    }
}

