package ru.veshvokrug.recommendation.service;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.redis.core.HashOperations;
import org.springframework.data.redis.core.StringRedisTemplate;

import java.util.LinkedHashMap;
import java.util.Map;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.Mockito.*;

/**
 * Тесты для {@link UserCategoryWeightService}.
 */
@ExtendWith(MockitoExtension.class)
class UserCategoryWeightServiceTest {

	@Mock
	private StringRedisTemplate redisTemplate;

	@Mock
	private HashOperations<String, Object, Object> hashOperations;

	private UserCategoryWeightService service;

	@BeforeEach
	void setUp() {
		when(redisTemplate.opsForHash()).thenReturn(hashOperations);
		service = new UserCategoryWeightService(redisTemplate);
	}

	@Test
	void shouldIncrementCategoryAtomically() {
		when(hashOperations.increment("user:u1:cat_weights", "sports", 0.7)).thenReturn(1.7);

		service.incrementCategoryWeight("u1", "sports", 0.7);

		verify(hashOperations).increment("user:u1:cat_weights", "sports", 0.7);
	}

	@Test
	void shouldClampNegativeCategoryWeightToZero() {
		when(hashOperations.increment("user:u1:cat_weights", "sports", -5.0)).thenReturn(-1.0);

		service.incrementCategoryWeight("u1", "sports", -5.0);

		verify(hashOperations).put("user:u1:cat_weights", "sports", "0.0");
	}

	@Test
	void shouldReturnTopCategoriesSortedByWeight() {
		Map<Object, Object> entries = new LinkedHashMap<>();
		entries.put("tools", "1.5");
		entries.put("sports", "3.0");
		entries.put("home", "2.0");
		when(hashOperations.entries("user:u1:cat_weights")).thenReturn(entries);

		Map<String, Double> result = service.getTopCategories("u1", 2);

		assertEquals(Map.of("sports", 3.0, "home", 2.0), result);
	}

	@Test
	void shouldApplyDecayAndCleanup() {
		Map<Object, Object> entries = new LinkedHashMap<>();
		entries.put("tools", "0.0005");
		entries.put("sports", "2.0");
		when(hashOperations.entries("user:u1:cat_weights")).thenReturn(entries);

		service.applyDecay("u1", 0.5);
		service.removeWeightsBelowThreshold("u1", 0.001);

		verify(hashOperations).put("user:u1:cat_weights", "tools", String.valueOf(0.00025));
		verify(hashOperations).put("user:u1:cat_weights", "sports", "1.0");
		verify(hashOperations).delete("user:u1:cat_weights", "tools");
	}

	@Test
	void shouldReturnEmptyWhenRedisFails() {
		when(hashOperations.entries("user:u1:cat_weights")).thenThrow(new RuntimeException("redis down"));

		assertTrue(service.getTopCategories("u1", 5).isEmpty());
		assertTrue(service.getAllCategories("u1").isEmpty());
	}
}

