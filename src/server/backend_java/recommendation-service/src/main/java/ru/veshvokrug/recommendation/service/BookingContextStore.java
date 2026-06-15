package ru.veshvokrug.recommendation.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.util.Optional;

/**
 * Хранилище контекста бронирования в Redis.
 *
 * Saga-события аренды кроме первого (RentalBookingRequested) не содержат
 * ни listingId, ни tenantId — только bookingId. Чтобы превращать
 * Approved/Cancelled-события в события рекомендаций, контекст первого
 * события сохраняется по bookingId и читается при последующих.
 *
 * @author Dmitrii Marchenko
 */
@Service
public class BookingContextStore {
    private static final Logger log = LoggerFactory.getLogger(BookingContextStore.class);
    private static final String KEY_PREFIX = "recommendation:booking-context:";

    private final StringRedisTemplate redisTemplate;
    private final ObjectMapper objectMapper;
    private final Duration ttl;

    public BookingContextStore(StringRedisTemplate redisTemplate,
                               ObjectMapper objectMapper,
                               @Value("${recommendation.integration.booking-context-ttl-days:90}") long ttlDays) {
        this.redisTemplate = redisTemplate;
        this.objectMapper = objectMapper;
        this.ttl = Duration.ofDays(ttlDays);
    }

    public void save(String bookingId, BookingContext context) {
        try {
            String value = objectMapper.writeValueAsString(context);
            redisTemplate.opsForValue().set(KEY_PREFIX + bookingId, value, ttl);
        } catch (Exception e) {
            // Потеря контекста означает лишь пропуск будущих weight-сигналов,
            // а не потерю бизнес-данных — поэтому не роняем обработку события
            log.warn("Не удалось сохранить контекст бронирования {}: {}", bookingId, e.getMessage());
        }
    }

    public Optional<BookingContext> find(String bookingId) {
        try {
            String value = redisTemplate.opsForValue().get(KEY_PREFIX + bookingId);
            if (value == null) {
                return Optional.empty();
            }
            return Optional.of(objectMapper.readValue(value, BookingContext.class));
        } catch (Exception e) {
            log.warn("Не удалось прочитать контекст бронирования {}: {}", bookingId, e.getMessage());
            return Optional.empty();
        }
    }

    /**
     * Контекст бронирования: кто бронировал, что и в какой категории.
     */
    public record BookingContext(String userId, String listingId, String categorySlug) {
    }
}
