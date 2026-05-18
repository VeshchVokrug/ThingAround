package ru.veshvokrug.recommendation.service;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.redis.core.StringRedisTemplate;
import ru.veshvokrug.recommendation.config.EventWeightsConfig;

import java.util.Set;

import static org.junit.jupiter.api.Assertions.assertDoesNotThrow;
import org.mockito.ArgumentCaptor;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.anyDouble;
import static org.mockito.Mockito.never;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

/**
 * Тесты для {@link WeightDecayScheduler}.
 */
@ExtendWith(MockitoExtension.class)
class WeightDecaySchedulerTest {

    @Mock
    private StringRedisTemplate redisTemplate;

    @Mock
    private UserCategoryWeightService userCategoryWeightService;

    @Mock
    private ListingPopularityService listingPopularityService;

    private WeightDecayScheduler scheduler;

    @BeforeEach
    void setUp() {
        EventWeightsConfig config = new EventWeightsConfig();
        when(redisTemplate.keys("user:*:cat_weights")).thenReturn(Set.of("user:u1:cat_weights"));
        when(redisTemplate.keys("pop:*")).thenReturn(Set.of("pop:sports"));
        scheduler = new WeightDecayScheduler(
                redisTemplate,
                config,
                userCategoryWeightService,
                listingPopularityService);
    }

    @Test
    void shouldApplyDecayToAllUsersAndCategories() {
        scheduler.applyUserCategoryDecay();
        scheduler.applyListingPopularityDecay();

        verify(userCategoryWeightService).applyDecay(org.mockito.ArgumentMatchers.eq("u1"), anyDouble());
        verify(userCategoryWeightService).removeWeightsBelowThreshold("u1", 0.001);
        verify(listingPopularityService).applyDecay(org.mockito.ArgumentMatchers.eq("sports"), anyDouble());
    }

    @Test
    void shouldSkipWhenNoKeysFound() {
        when(redisTemplate.keys("user:*:cat_weights")).thenReturn(Set.of());
        when(redisTemplate.keys("pop:*")).thenReturn(Set.of());

        scheduler.applyUserCategoryDecay();
        scheduler.applyListingPopularityDecay();

        verify(userCategoryWeightService, never()).applyDecay(org.mockito.ArgumentMatchers.anyString(), anyDouble());
        verify(listingPopularityService, never()).applyDecay(org.mockito.ArgumentMatchers.anyString(), anyDouble());
    }

    @Test
    void shouldNotFailOnMalformedRedisKeys() {
        when(redisTemplate.keys("user:*:cat_weights")).thenReturn(Set.of("broken-user-key"));
        when(redisTemplate.keys("pop:*")).thenReturn(Set.of("broken-pop-key"));

        assertDoesNotThrow(() -> scheduler.applyUserCategoryDecay());
        assertDoesNotThrow(() -> scheduler.applyListingPopularityDecay());
    }

    @Test
    void shouldUseNoDecayWhenHalfLifeIsNonPositive() {
        EventWeightsConfig config = new EventWeightsConfig();
        config.setUserInterestHalfLifeDays(0);
        config.setListingPopularityHalfLifeDays(-1);

        scheduler = new WeightDecayScheduler(
                redisTemplate,
                config,
                userCategoryWeightService,
                listingPopularityService);

        scheduler.applyUserCategoryDecay();
        scheduler.applyListingPopularityDecay();

        ArgumentCaptor<Double> decayCaptor = ArgumentCaptor.forClass(Double.class);
        verify(userCategoryWeightService)
                .applyDecay(org.mockito.ArgumentMatchers.eq("u1"), decayCaptor.capture());
        assertEquals(1.0, decayCaptor.getValue(), 1e-9);

        ArgumentCaptor<Double> popDecayCaptor = ArgumentCaptor.forClass(Double.class);
        verify(listingPopularityService)
                .applyDecay(org.mockito.ArgumentMatchers.eq("sports"), popDecayCaptor.capture());
        assertEquals(1.0, popDecayCaptor.getValue(), 1e-9);
    }
}

