package ru.veshvokrug.recommendation.service;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.data.redis.core.HashOperations;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.data.redis.core.script.RedisScript;

import java.util.LinkedHashMap;
import java.util.List;
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
		service = new UserCategoryWeightService(redisTemplate);
	}

	@Test
	@SuppressWarnings("unchecked")
	void shouldIncrementCategoryAtomicallyViaScript() {
		// Инкремент с clamp до нуля выполняется одним Lua-скриптом,
		// сами границы проверяются скриптом на стороне Redis
		service.incrementCategoryWeight("u1", "sports", 0.7);

		verify(redisTemplate).execute(
				any(RedisScript.class),
				eq(List.of("user:u1:cat_weights")),
				eq("sports"),
				eq("0.7"));
	}

	@Test
	void shouldReturnTopCategoriesSortedByWeight() {
		when(redisTemplate.opsForHash()).thenReturn(hashOperations);
		Map<Object, Object> entries = new LinkedHashMap<>();
		entries.put("tools", "1.5");
		entries.put("sports", "3.0");
		entries.put("home", "2.0");
		when(hashOperations.entries("user:u1:cat_weights")).thenReturn(entries);

		Map<String, Double> result = service.getTopCategories("u1", 2);

		assertEquals(Map.of("sports", 3.0, "home", 2.0), result);
	}

	@Test
	@SuppressWarnings("unchecked")
	void shouldApplyDecayAndCleanupViaSingleScript() {
		when(redisTemplate.execute(any(RedisScript.class), eq(List.of("user:u1:cat_weights")), any(), any()))
				.thenReturn(1L);

		long removed = service.applyDecay("u1", 0.5, 0.001);

		assertEquals(1L, removed);
		verify(redisTemplate).execute(
				any(RedisScript.class),
				eq(List.of("user:u1:cat_weights")),
				eq("0.5"),
				eq("0.001"));
	}

	@Test
	void shouldReturnEmptyWhenRedisFails() {
		when(redisTemplate.opsForHash()).thenReturn(hashOperations);
		when(hashOperations.entries("user:u1:cat_weights")).thenThrow(new RuntimeException("redis down"));

		assertTrue(service.getTopCategories("u1", 5).isEmpty());
		assertTrue(service.getAllCategories("u1").isEmpty());
	}
}

