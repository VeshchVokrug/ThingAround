package ru.veshvokrug.coownership.input.kafka;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.stereotype.Component;
import ru.veshvokrug.coownership.service.InboundEventIdempotencyService;
import ru.veshvokrug.coownership.service.PeriodLifecycleService;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.UUID;

/**
 * Kafka-consumer для входящих событий каталога и аренды.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Component
public class CoownershipInboundKafkaConsumer {
    private static final Logger log = LoggerFactory.getLogger(CoownershipInboundKafkaConsumer.class);
    private static final String CONSUMER_RENTAL_LISTING_CREATED = "coownership-rental-listing-created";
    private static final String CONSUMER_BOOKING_CONFIRMED = "coownership-booking-confirmed";

    private final ObjectMapper objectMapper;
    private final InboundEventIdempotencyService idempotencyService;
    private final PeriodLifecycleService periodLifecycleService;

    public CoownershipInboundKafkaConsumer(ObjectMapper objectMapper,
                                           InboundEventIdempotencyService idempotencyService,
                                           PeriodLifecycleService periodLifecycleService) {
        this.objectMapper = objectMapper;
        this.idempotencyService = idempotencyService;
        this.periodLifecycleService = periodLifecycleService;
    }

    @KafkaListener(
            topics = "${coownership.kafka.inbound.rental-listing-created-topic:rental-listing-created}",
            groupId = "${coownership.kafka.inbound.group-id:coownership-service}"
    )
    public void onRentalListingCreated(String rawMessage) {
        RentalListingCreatedEvent event = parseOrSkip(rawMessage, RentalListingCreatedEvent.class);
        if (event == null) {
            return;
        }
        idempotencyService.executeOnce(event.eventId(), CONSUMER_RENTAL_LISTING_CREATED, () ->
                periodLifecycleService.linkRentalListing(event.coownershipListingId(), event.rentalListingId())
        );
    }

    @KafkaListener(
            topics = "${coownership.kafka.inbound.booking-confirmed-topic:booking-confirmed}",
            groupId = "${coownership.kafka.inbound.group-id:coownership-service}"
    )
    public void onBookingConfirmed(String rawMessage) {
        BookingConfirmedEvent event = parseOrSkip(rawMessage, BookingConfirmedEvent.class);
        if (event == null) {
            return;
        }
        idempotencyService.executeOnce(event.eventId(), CONSUMER_BOOKING_CONFIRMED, () ->
                periodLifecycleService.applyBookingConfirmed(
                        event.rentalListingId(),
                        event.startDate(),
                        event.endDate(),
                        event.totalPrice()
                )
        );
    }

    private <T> T parseOrSkip(String rawMessage, Class<T> type) {
        try {
            return objectMapper.readValue(rawMessage, type);
        } catch (Exception ex) {
            log.error("Skipping malformed Kafka event for {}: {}", type.getSimpleName(), ex.getMessage());
            return null;
        }
    }

    private record RentalListingCreatedEvent(UUID eventId,
                                             UUID coownershipListingId,
                                             UUID rentalListingId) {
    }

    private record BookingConfirmedEvent(UUID eventId,
                                         UUID rentalListingId,
                                         LocalDate startDate,
                                         LocalDate endDate,
                                         BigDecimal totalPrice) {
    }
}
