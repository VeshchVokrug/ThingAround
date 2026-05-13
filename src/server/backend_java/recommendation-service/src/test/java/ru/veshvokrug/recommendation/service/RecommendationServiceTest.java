package ru.veshvokrug.recommendation.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.data.redis.core.ValueOperations;
import ru.veshvokrug.recommendation.config.EventWeightsConfig;

import java.util.List;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.concurrent.TimeUnit;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyInt;
import static org.mockito.ArgumentMatchers.anyLong;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

/**
 * Тесты для {@link RecommendationService}.
 */
@ExtendWith(MockitoExtension.class)
class RecommendationServiceTest {

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
    void shouldReturnCachedRecommendationsWhenCacheExists() throws Exception {
        when(valueOperations.get("rec:user-1:4")).thenReturn("[\"l1\",\"l2\"]");

        List<String> result = recommendationService.getRecommendations("user-1", 4);

        assertEquals(List.of("l1", "l2"), result);
        verifyNoInteractions(userCategoryWeightService, listingPopularityService);
        verify(valueOperations, never()).set(any(), any(), anyLong(), any());
    }

    @Test
    void shouldBuildRecommendationsAndStoreThemInCache() throws Exception {
        when(valueOperations.get("rec:user-1:4")).thenReturn(null);
        Map<String, Double> categories = new LinkedHashMap<>();
        categories.put("sports", 3.0);
        categories.put("tools", 2.0);
        when(userCategoryWeightService.getTopCategories("user-1", 2)).thenReturn(categories);
        when(listingPopularityService.getTopListings(eq("sports"), anyInt())).thenReturn(List.of("s1", "s2"));
        when(listingPopularityService.getTopListings(eq("tools"), anyInt())).thenReturn(List.of("t1", "t2"));

        List<String> result = recommendationService.getRecommendations("user-1", 4);

        assertEquals(4, result.size());
        assertEquals(List.of("s1", "t1", "s2", "t2"), result);

        ArgumentCaptor<String> payloadCaptor = ArgumentCaptor.forClass(String.class);
        verify(valueOperations).set(eq("rec:user-1:4"), payloadCaptor.capture(), eq(300L), eq(TimeUnit.SECONDS));
        assertTrue(payloadCaptor.getValue().contains("s1"));
        verify(userCategoryWeightService).getTopCategories("user-1", 2);
        verify(listingPopularityService, times(2)).getTopListings(anyString(), anyInt());
    }

    @Test
    void shouldReturnEmptyListForBlankUserId() {
        assertEquals(List.of(), recommendationService.getRecommendations("  ", 10));
    }

    @Test
    void shouldUseDefaultSizeAndReturnEmptyListWhenNoCategoriesFound() {
        when(valueOperations.get("rec:user-1:4")).thenReturn(null);
        when(userCategoryWeightService.getTopCategories("user-1", 2)).thenReturn(Map.of());

        List<String> result = recommendationService.getRecommendations("user-1", 0);

        assertEquals(List.of(), result);
        verify(userCategoryWeightService).getTopCategories("user-1", 2);
        verifyNoInteractions(listingPopularityService);
        verify(valueOperations, never()).set(any(), any(), anyLong(), any());
    }

    @Test
    void shouldUseConfiguredTopListingsPerCategoryWhenExplicitlySet() {
        EventWeightsConfig config = new EventWeightsConfig();
        config.setTopCategoriesCount(2);
        config.setDefaultRecommendationSize(6);
        config.setTopListingsPerCategory(1);
        config.setRecommendationCacheTtlSeconds(300);
        recommendationService = new RecommendationService(
                userCategoryWeightService,
                listingPopularityService,
                config,
                redisTemplate,
                new ObjectMapper()
        );

        when(valueOperations.get("rec:user-1:6")).thenReturn(null);
        Map<String, Double> categories = new LinkedHashMap<>();
        categories.put("sports", 3.0);
        categories.put("tools", 2.0);
        when(userCategoryWeightService.getTopCategories("user-1", 2)).thenReturn(categories);
        when(listingPopularityService.getTopListings("sports", 1)).thenReturn(List.of("s1"));
        when(listingPopularityService.getTopListings("tools", 1)).thenReturn(List.of("t1"));

        List<String> result = recommendationService.getRecommendations("user-1", 6);

        assertEquals(List.of("s1", "t1"), result);
        verify(listingPopularityService).getTopListings("sports", 1);
        verify(listingPopularityService).getTopListings("tools", 1);
    }

    @Test
    void shouldSkipDuplicateListingsAcrossCategories() {
        when(valueOperations.get("rec:user-1:4")).thenReturn(null);
        Map<String, Double> categories = new LinkedHashMap<>();
        categories.put("sports", 3.0);
        categories.put("tools", 2.0);
        when(userCategoryWeightService.getTopCategories("user-1", 2)).thenReturn(categories);
        when(listingPopularityService.getTopListings("sports", 2)).thenReturn(List.of("same", "s2"));
        when(listingPopularityService.getTopListings("tools", 2)).thenReturn(List.of("same", "t2"));

        List<String> result = recommendationService.getRecommendations("user-1", 4);

        assertEquals(List.of("same", "s2", "t2"), result);
    }

    @Test
    void shouldFallbackToRebuildWhenCachePayloadIsBroken() {
        when(valueOperations.get("rec:user-1:4")).thenReturn("not-json");
        Map<String, Double> categories = new LinkedHashMap<>();
        categories.put("sports", 3.0);
        when(userCategoryWeightService.getTopCategories("user-1", 2)).thenReturn(categories);
        when(listingPopularityService.getTopListings("sports", 4)).thenReturn(List.of("s1", "s2"));

        List<String> result = recommendationService.getRecommendations("user-1", 4);

        assertEquals(List.of("s1", "s2"), result);
        verify(userCategoryWeightService).getTopCategories("user-1", 2);
    }
}

