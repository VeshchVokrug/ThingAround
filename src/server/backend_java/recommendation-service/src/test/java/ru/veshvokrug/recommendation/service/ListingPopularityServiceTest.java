package ru.veshvokrug.recommendation.service;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.data.redis.core.ZSetOperations;

import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

/**
 * Тесты для {@link ListingPopularityService}.
 */
@ExtendWith(MockitoExtension.class)
class ListingPopularityServiceTest {

    @Mock
    private StringRedisTemplate redisTemplate;

    @Mock
    private ZSetOperations<String, String> zSetOperations;

    private ListingPopularityService service;

    @BeforeEach
    void setUp() {
        when(redisTemplate.opsForZSet()).thenReturn(zSetOperations);
        service = new ListingPopularityService(redisTemplate);
    }

    @Test
    void shouldIncrementPopularityAtomicallyAndClampToZero() {
        when(zSetOperations.incrementScore("pop:sports", "l1", -2.0)).thenReturn(-1.0);

        service.incrementListingPopularity("sports", "l1", -2.0);

        verify(zSetOperations).incrementScore("pop:sports", "l1", -2.0);
        verify(zSetOperations).add("pop:sports", "l1", 0.0);
    }

    @Test
    void shouldReturnTopListings() {
        Set<String> listings = new LinkedHashSet<>(List.of("l1", "l2"));
        when(zSetOperations.reverseRange("pop:sports", 0, 1L)).thenReturn(listings);

        assertEquals(List.of("l1", "l2"), service.getTopListings("sports", 2));
    }

    @Test
    void shouldReturnEmptyTopListingsWhenNothingFound() {
        when(zSetOperations.reverseRange("pop:sports", 0, 1L)).thenReturn(Set.of());
        assertTrue(service.getTopListings("sports", 2).isEmpty());
    }

    @Test
    void shouldReturnScoresAndApplyDecay() {
        ZSetOperations.TypedTuple<String> tuple1 = mock(ZSetOperations.TypedTuple.class);
        when(tuple1.getValue()).thenReturn("l1");
        when(tuple1.getScore()).thenReturn(10.0);

        ZSetOperations.TypedTuple<String> tuple2 = mock(ZSetOperations.TypedTuple.class);
        when(tuple2.getValue()).thenReturn("l2");
        when(tuple2.getScore()).thenReturn(7.0);

        Set<ZSetOperations.TypedTuple<String>> scored = Set.of(tuple1, tuple2);
        when(zSetOperations.reverseRangeWithScores("pop:sports", 0, 1L)).thenReturn(scored);
        when(zSetOperations.rangeWithScores("pop:sports", 0, -1)).thenReturn(scored);

        Map<String, Double> withScores = service.getTopListingsWithScores("sports", 2);
        assertEquals(2, withScores.size());

        service.applyDecay("sports", 0.5);
        verify(zSetOperations).add(eq("pop:sports"), eq("l1"), eq(5.0));
        verify(zSetOperations).add(eq("pop:sports"), eq("l2"), eq(3.5));
    }

    @Test
    void shouldReturnZeroCountWhenRedisReturnsNull() {
        when(zSetOperations.size("pop:sports")).thenReturn(null);
        assertEquals(0, service.getListingCount("sports"));
    }

    @Test
    void shouldReturnEmptyWhenRedisThrowsError() {
        when(zSetOperations.reverseRange("pop:sports", 0, 2L)).thenThrow(new RuntimeException("redis down"));
        assertTrue(service.getTopListings("sports", 3).isEmpty());
    }
}

