package ru.veshvokrug.recommendation.service;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.redis.core.Cursor;
import org.springframework.data.redis.core.ScanOptions;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.data.redis.core.ValueOperations;
import org.springframework.data.redis.core.script.RedisScript;
import ru.veshvokrug.recommendation.config.EventWeightsConfig;

import java.time.Duration;
import java.util.Iterator;
import java.util.List;

import static org.junit.jupiter.api.Assertions.assertDoesNotThrow;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyDouble;
import static org.mockito.ArgumentMatchers.anyList;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.lenient;
import static org.mockito.Mockito.mock;
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
    private ValueOperations<String, String> valueOperations;

    @Mock
    private UserCategoryWeightService userCategoryWeightService;

    @Mock
    private ListingPopularityService listingPopularityService;

    private WeightDecayScheduler scheduler;

    @BeforeEach
    @SuppressWarnings("unchecked")
    void setUp() {
        lenient().when(redisTemplate.opsForValue()).thenReturn(valueOperations);
        // Лок свободен по умолчанию
        lenient().when(valueOperations.setIfAbsent(anyString(), anyString(), any(Duration.class)))
                .thenReturn(true);
        lenient().when(redisTemplate.execute(any(RedisScript.class), anyList(), any()))
                .thenReturn(1L);
        scheduler = new WeightDecayScheduler(
                redisTemplate,
                new EventWeightsConfig(),
                userCategoryWeightService,
                listingPopularityService);
    }

    @Test
    void shouldApplyDecayToAllUsersAndCategories() {
        stubScan("user:*:cat_weights", List.of("user:u1:cat_weights"));
        stubScan("pop:*", List.of("pop:sports"));

        scheduler.applyUserCategoryDecay();
        scheduler.applyListingPopularityDecay();

        verify(userCategoryWeightService).applyDecay(eq("u1"), anyDouble(), eq(0.001));
        verify(listingPopularityService).applyDecay(eq("sports"), anyDouble());
    }

    @Test
    void shouldDoNothingWhenNoKeysFound() {
        stubScan("user:*:cat_weights", List.of());
        stubScan("pop:*", List.of());

        scheduler.applyUserCategoryDecay();
        scheduler.applyListingPopularityDecay();

        verify(userCategoryWeightService, never()).applyDecay(anyString(), anyDouble(), anyDouble());
        verify(listingPopularityService, never()).applyDecay(anyString(), anyDouble());
    }

    @Test
    void shouldSkipTaskWhenLockIsTaken() {
        when(valueOperations.setIfAbsent(anyString(), anyString(), any(Duration.class)))
                .thenReturn(false);

        scheduler.applyUserCategoryDecay();
        scheduler.applyListingPopularityDecay();

        verify(userCategoryWeightService, never()).applyDecay(anyString(), anyDouble(), anyDouble());
        verify(listingPopularityService, never()).applyDecay(anyString(), anyDouble());
    }

    @Test
    void shouldSkipDecayWhenHalfLifeIsNonPositive() {
        // multiplier == 1.0 означает «ничего не меняем» — обход ключей не нужен
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

        verify(userCategoryWeightService, never()).applyDecay(anyString(), anyDouble(), anyDouble());
        verify(listingPopularityService, never()).applyDecay(anyString(), anyDouble());
    }

    @Test
    void shouldNotFailWhenScanThrows() {
        when(redisTemplate.scan(any(ScanOptions.class))).thenThrow(new RuntimeException("redis down"));

        assertDoesNotThrow(() -> scheduler.applyUserCategoryDecay());
        assertDoesNotThrow(() -> scheduler.applyListingPopularityDecay());
    }

    @SuppressWarnings("unchecked")
    private void stubScan(String pattern, List<String> keys) {
        Cursor<String> cursor = mock(Cursor.class);
        Iterator<String> iterator = keys.iterator();
        lenient().when(cursor.hasNext()).thenAnswer(inv -> iterator.hasNext());
        lenient().when(cursor.next()).thenAnswer(inv -> iterator.next());
        lenient().when(redisTemplate.scan(org.mockito.ArgumentMatchers.argThat(
                (ScanOptions options) -> options != null && pattern.equals(options.getPattern()))))
                .thenReturn(cursor);
    }
}
