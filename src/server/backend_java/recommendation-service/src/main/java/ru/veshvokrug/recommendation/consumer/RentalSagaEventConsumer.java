package ru.veshvokrug.recommendation.consumer;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;
import ru.veshvokrug.recommendation.config.RabbitMQConfig;
import ru.veshvokrug.recommendation.event.RecommendationEventDto;
import ru.veshvokrug.recommendation.publisher.RecommendationEventPublisher;
import ru.veshvokrug.recommendation.service.BookingContextStore;
import ru.veshvokrug.recommendation.service.BookingContextStore.BookingContext;
import ru.veshvokrug.recommendation.service.CatalogCategoryResolver;

import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.util.Optional;
import java.util.UUID;

/**
 * Мост saga-событий аренды в события рекомендаций (anti-corruption layer).
 *
 * RentalService публикует события жизненного цикла бронирования через
 * MassTransit, который оборачивает сообщение в JSON-конверт с полем
 * messageType (URN .NET-типа). Мост разбирает конверт, обогащает событие
 * категорией листинга (через каталог и Redis-контекст бронирования)
 * и публикует обычный {@link RecommendationEventDto} во внутренний
 * exchange — дальше работает существующий пайплайн весов.
 *
 * Маппинг:
 * RentalBookingRequestedEvent -> BookingCreated,
 * RentalBookingApprovedEvent  -> BookingConfirmed,
 * RentalBookingCancelled/Rejected/ExpiredEvent -> BookingCancelled.
 *
 * @author Dmitrii Marchenko
 */
@Component
public class RentalSagaEventConsumer {
    private static final Logger log = LoggerFactory.getLogger(RentalSagaEventConsumer.class);

    private static final String URN_PREFIX = "urn:message:Core.SAGA.Contracts.Events:";
    private static final String BOOKING_REQUESTED = URN_PREFIX + "RentalBookingRequestedEvent";
    private static final String BOOKING_APPROVED = URN_PREFIX + "RentalBookingApprovedEvent";
    private static final String BOOKING_REJECTED = URN_PREFIX + "RentalBookingRejectedEvent";
    private static final String BOOKING_CANCELLED = URN_PREFIX + "RentalBookingCancelledEvent";
    private static final String BOOKING_EXPIRED = URN_PREFIX + "RentalBookingExpiredEvent";

    private final ObjectMapper objectMapper;
    private final CatalogCategoryResolver categoryResolver;
    private final BookingContextStore bookingContextStore;
    private final RecommendationEventPublisher eventPublisher;

    public RentalSagaEventConsumer(ObjectMapper objectMapper,
                                   CatalogCategoryResolver categoryResolver,
                                   BookingContextStore bookingContextStore,
                                   RecommendationEventPublisher eventPublisher) {
        this.objectMapper = objectMapper;
        this.categoryResolver = categoryResolver;
        this.bookingContextStore = bookingContextStore;
        this.eventPublisher = eventPublisher;
    }

    @RabbitListener(queues = RabbitMQConfig.RENTAL_EVENTS_QUEUE)
    public void onRentalSagaEvent(Message message) {
        JsonNode envelope = parseOrSkip(message);
        if (envelope == null) {
            return;
        }

        String messageType = resolveConcreteType(envelope);
        JsonNode body = envelope.path("message");
        if (messageType == null || body.isMissingNode()) {
            log.debug("Пропускаю сообщение без известного messageType");
            return;
        }

        switch (messageType) {
            case BOOKING_REQUESTED -> handleBookingRequested(envelope, body);
            case BOOKING_APPROVED -> handleWithContext(envelope, body, "BookingConfirmed");
            case BOOKING_REJECTED, BOOKING_CANCELLED, BOOKING_EXPIRED ->
                    handleWithContext(envelope, body, "BookingCancelled");
            default -> log.debug("Пропускаю неизвестный тип saga-события: {}", messageType);
        }
    }

    private void handleBookingRequested(JsonNode envelope, JsonNode body) {
        String bookingId = body.path("bookingId").asText(null);
        String listingId = body.path("listingId").asText(null);
        String tenantId = body.path("tenantId").asText(null);
        if (bookingId == null || listingId == null || tenantId == null) {
            log.warn("RentalBookingRequestedEvent без обязательных полей: {}", body);
            return;
        }

        String categorySlug = categoryResolver.resolveCategorySlug(listingId).orElse(null);
        // Контекст сохраняем даже без категории: попытка её получить
        // повторится не будет, но связка пользователь-листинг останется
        bookingContextStore.save(bookingId, new BookingContext(tenantId, listingId, categorySlug));

        if (categorySlug == null) {
            log.warn("Категория листинга {} не получена — событие BookingCreated пропущено", listingId);
            return;
        }

        publish(envelope, "BookingCreated", tenantId, categorySlug, listingId);
    }

    private void handleWithContext(JsonNode envelope, JsonNode body, String eventType) {
        String bookingId = body.path("bookingId").asText(null);
        if (bookingId == null) {
            log.warn("Saga-событие без bookingId: {}", body);
            return;
        }

        Optional<BookingContext> context = bookingContextStore.find(bookingId);
        if (context.isEmpty() || context.get().categorySlug() == null) {
            log.debug("Нет контекста бронирования {} — событие {} пропущено", bookingId, eventType);
            return;
        }

        BookingContext ctx = context.get();
        publish(envelope, eventType, ctx.userId(), ctx.categorySlug(), ctx.listingId());
    }

    private void publish(JsonNode envelope, String eventType, String userId, String categorySlug, String listingId) {
        RecommendationEventDto event = new RecommendationEventDto(
                envelope.path("messageId").asText(UUID.randomUUID().toString()),
                userId,
                eventType,
                categorySlug,
                listingId,
                resolveTimestamp(envelope)
        );
        eventPublisher.publish(event);
        log.debug("Saga-событие преобразовано: type={}, userId={}, listingId={}", eventType, userId, listingId);
    }

    private long resolveTimestamp(JsonNode envelope) {
        try {
            return Instant.parse(envelope.path("sentTime").asText()).toEpochMilli();
        } catch (Exception ignored) {
            return System.currentTimeMillis();
        }
    }

    private String resolveConcreteType(JsonNode envelope) {
        for (JsonNode urn : envelope.path("messageType")) {
            String value = urn.asText();
            if (value.startsWith(URN_PREFIX) && !value.endsWith(":IRentalEvents")) {
                return value;
            }
        }
        return null;
    }

    private JsonNode parseOrSkip(Message message) {
        try {
            return objectMapper.readTree(new String(message.getBody(), StandardCharsets.UTF_8));
        } catch (Exception e) {
            log.error("Пропускаю некорректное saga-сообщение: {}", e.getMessage());
            return null;
        }
    }
}
