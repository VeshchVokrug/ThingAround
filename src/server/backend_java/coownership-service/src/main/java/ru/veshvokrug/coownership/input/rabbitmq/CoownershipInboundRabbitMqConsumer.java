package ru.veshvokrug.coownership.input.rabbitmq;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;
import ru.veshvokrug.coownership.service.InboundEventIdempotencyService;
import ru.veshvokrug.coownership.service.PeriodLifecycleService;

import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.time.LocalDate;
import java.util.UUID;

/**
 * Consumer MassTransit-событий жизненного цикла бронирования от RentalService.
 *
 * RentalService публикует через MassTransit IPublishEndpoint.Publish() — сообщения
 * приходят в конверте с полем messageType (URN .NET-типа). Consumer разбирает конверт
 * и диспатчит по конкретному типу события.
 *
 * Маппинг событий:
 * RentalBookingApprovedEvent  → applyBookingConfirmed (доход + BOOKED-слоты)
 * RentalBookingRequestedEvent → linkRentalListing (связать период с rental listing)
 *
 * Остальные события саги (Cancelled, Rejected, Expired) для бизнес-логики совладения
 * не нужны — bookingId без listingId не даёт достаточно данных для действий.
 *
 * @author Dmitrii Marchenko
 */
@Component
public class CoownershipInboundRabbitMqConsumer {
    private static final Logger log = LoggerFactory.getLogger(CoownershipInboundRabbitMqConsumer.class);

    private static final String URN_PREFIX = "urn:message:Core.SAGA.Contracts.Events:";
    private static final String BOOKING_REQUESTED = URN_PREFIX + "RentalBookingRequestedEvent";
    private static final String BOOKING_APPROVED  = URN_PREFIX + "RentalBookingApprovedEvent";

    // Имена консьюмеров для таблицы processed_events (идемпотентность)
    private static final String CONSUMER_BOOKING_REQUESTED = "coownership-rental-listing-created";
    private static final String CONSUMER_BOOKING_APPROVED  = "coownership-booking-confirmed";

    private final ObjectMapper objectMapper;
    private final InboundEventIdempotencyService idempotencyService;
    private final PeriodLifecycleService periodLifecycleService;

    public CoownershipInboundRabbitMqConsumer(ObjectMapper objectMapper,
                                              InboundEventIdempotencyService idempotencyService,
                                              PeriodLifecycleService periodLifecycleService) {
        this.objectMapper = objectMapper;
        this.idempotencyService = idempotencyService;
        this.periodLifecycleService = periodLifecycleService;
    }

    @RabbitListener(queues = "${coownership.rabbitmq.inbound.rental-events-queue:coownership.rental-events}")
    public void onRentalEvent(Message message) {
        JsonNode envelope = parseOrSkip(message);
        if (envelope == null) {
            return;
        }

        String messageType = resolveConcreteType(envelope);
        JsonNode body = envelope.path("message");
        if (messageType == null || body.isMissingNode()) {
            log.debug("Пропускаю rental-событие без известного messageType");
            return;
        }

        switch (messageType) {
            case BOOKING_REQUESTED -> handleBookingRequested(envelope, body);
            case BOOKING_APPROVED  -> handleBookingApproved(envelope, body);
            default -> log.debug("Пропускаю rental-событие типа: {}", messageType);
        }
    }

    /**
     * RentalBookingRequestedEvent: связываем ACTIVE-период с rental-листингом.
     * bookingId используется как eventId для идемпотентности.
     */
    private void handleBookingRequested(JsonNode envelope, JsonNode body) {
        String listingIdStr = body.path("ListingId").asText(null);
        String bookingIdStr = body.path("BookingId").asText(null);

        if (listingIdStr == null || bookingIdStr == null) {
            log.warn("RentalBookingRequestedEvent без обязательных полей: {}", body);
            return;
        }

        UUID bookingId = parseUuid(bookingIdStr);
        UUID listingId = parseUuid(listingIdStr);
        if (bookingId == null || listingId == null) {
            return;
        }

        // listingId в контракте — это rental-листинг в каталоге; нужно найти
        // coownership-листинг, у которого catalogListingId == этому listingId
        idempotencyService.executeOnce(bookingId, CONSUMER_BOOKING_REQUESTED, () ->
                periodLifecycleService.linkRentalListingByCatalogId(listingId, bookingId)
        );
    }

    /**
     * RentalBookingApprovedEvent: применяем доход от бронирования.
     * Детали бронирования берём из контекста, сохранённого при Requested.
     */
    private void handleBookingApproved(JsonNode envelope, JsonNode body) {
        String bookingIdStr = body.path("BookingId").asText(null);
        if (bookingIdStr == null) {
            log.warn("RentalBookingApprovedEvent без BookingId: {}", body);
            return;
        }

        UUID bookingId = parseUuid(bookingIdStr);
        if (bookingId == null) {
            return;
        }

        idempotencyService.executeOnce(bookingId, CONSUMER_BOOKING_APPROVED, () ->
                periodLifecycleService.applyBookingApproved(bookingId)
        );
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
            log.error("Некорректный формат rental-события: {}", e.getMessage());
            return null;
        }
    }

    private UUID parseUuid(String value) {
        try {
            return UUID.fromString(value);
        } catch (Exception e) {
            log.warn("Некорректный UUID '{}': {}", value, e.getMessage());
            return null;
        }
    }
}
