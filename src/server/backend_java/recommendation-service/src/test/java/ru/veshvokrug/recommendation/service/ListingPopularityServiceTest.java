package ru.veshvokrug.recommendation.service;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.data.redis.core.ZSetOperations;
import org.springframework.data.redis.core.script.RedisScript;

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
        service = new ListingPopularityService(redisTemplate);
    }

    @Test
    @SuppressWarnings("unchecked")
    void shouldIncrementPopularityViaClampingScript() {
        service.incrementListingPopularity("sports", "l1", -2.0);

        verify(redisTemplate).execute(
                any(RedisScript.class),
                eq(List.of("pop:sports")),
                eq("l1"),
                eq("-2.0"));
    }

    @Test
    void shouldReturnEmptyTopListingsForNonPositiveLimit() {
        // При topM <= 0 запрос в Redis не выполняется: иначе диапазон (0, -1)
        // вернул бы все объявления категории
        assertTrue(service.getTopListings("sports", 0).isEmpty());
        assertTrue(service.getTopListingsWithScores("sports", -1).isEmpty());
        verifyNoInteractions(redisTemplate);
    }

    @Test
    void shouldReturnTopListings() {
        when(redisTemplate.opsForZSet()).thenReturn(zSetOperations);
        Set<String> listings = new LinkedHashSet<>(List.of("l1", "l2"));
        when(zSetOperations.reverseRange("pop:sports", 0, 1L)).thenReturn(listings);

        assertEquals(List.of("l1", "l2"), service.getTopListings("sports", 2));
    }

    @Test
    void shouldReturnEmptyTopListingsWhenNothingFound() {
        when(redisTemplate.opsForZSet()).thenReturn(zSetOperations);
        when(zSetOperations.reverseRange("pop:sports", 0, 1L)).thenReturn(Set.of());
        assertTrue(service.getTopListings("sports", 2).isEmpty());
    }

    @Test
    @SuppressWarnings("unchecked")
    void shouldReturnScoresAndApplyDecayViaScript() {
        when(redisTemplate.opsForZSet()).thenReturn(zSetOperations);
        ZSetOperations.TypedTuple<String> tuple1 = mock(ZSetOperations.TypedTuple.class);
        when(tuple1.getValue()).thenReturn("l1");
        when(tuple1.getScore()).thenReturn(10.0);

        ZSetOperations.TypedTuple<String> tuple2 = mock(ZSetOperations.TypedTuple.class);
        when(tuple2.getValue()).thenReturn("l2");
        when(tuple2.getScore()).thenReturn(7.0);

        Set<ZSetOperations.TypedTuple<String>> scored = Set.of(tuple1, tuple2);
        when(zSetOperations.reverseRangeWithScores("pop:sports", 0, 1L)).thenReturn(scored);

        Map<String, Double> withScores = service.getTopListingsWithScores("sports", 2);
        assertEquals(2, withScores.size());

        service.applyDecay("sports", 0.5);
        verify(redisTemplate).execute(
                any(RedisScript.class),
                eq(List.of("pop:sports")),
                eq("0.5"));
    }

    @Test
    void shouldReturnZeroCountWhenRedisReturnsNull() {
        when(redisTemplate.opsForZSet()).thenReturn(zSetOperations);
        when(zSetOperations.size("pop:sports")).thenReturn(null);
        assertEquals(0, service.getListingCount("sports"));
    }

    @Test
    void shouldReturnEmptyWhenRedisThrowsError() {
        when(redisTemplate.opsForZSet()).thenReturn(zSetOperations);
        when(zSetOperations.reverseRange("pop:sports", 0, 2L))
                .thenThrow(new RuntimeException("redis down"));
        assertTrue(service.getTopListings("sports", 3).isEmpty());
    }
}

