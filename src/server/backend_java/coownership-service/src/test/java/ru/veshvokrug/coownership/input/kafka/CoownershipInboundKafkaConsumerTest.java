package ru.veshvokrug.coownership.input.kafka;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import org.junit.jupiter.api.Test;
import ru.veshvokrug.coownership.service.InboundEventIdempotencyService;
import ru.veshvokrug.coownership.service.PeriodLifecycleService;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThatCode;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

class CoownershipInboundKafkaConsumerTest {

    @Test
    void onRentalListingCreatedExecutesLifecycleActionViaIdempotencyWrapper() {
        InboundEventIdempotencyService idempotencyService = mock(InboundEventIdempotencyService.class);
        PeriodLifecycleService periodLifecycleService = mock(PeriodLifecycleService.class);
        CoownershipInboundKafkaConsumer consumer = new CoownershipInboundKafkaConsumer(
                new ObjectMapper().registerModule(new JavaTimeModule()),
                idempotencyService,
                periodLifecycleService
        );

        UUID eventId = UUID.randomUUID();
        UUID coownershipListingId = UUID.randomUUID();
        UUID rentalListingId = UUID.randomUUID();
        String message = """
                {
                  "eventId": "%s",
                  "coownershipListingId": "%s",
                  "rentalListingId": "%s"
                }
                """.formatted(eventId, coownershipListingId, rentalListingId);

        doAnswer(invocation -> {
            Runnable action = invocation.getArgument(2);
            action.run();
            return null;
        }).when(idempotencyService).executeOnce(any(UUID.class), anyString(), any(Runnable.class));

        consumer.onRentalListingCreated(message);

        verify(idempotencyService).executeOnce(eq(eventId),
                eq("coownership-rental-listing-created"), any(Runnable.class));
        verify(periodLifecycleService).linkRentalListing(coownershipListingId, rentalListingId);
    }

    @Test
    void onBookingConfirmedExecutesLifecycleActionViaIdempotencyWrapper() {
        InboundEventIdempotencyService idempotencyService = mock(InboundEventIdempotencyService.class);
        PeriodLifecycleService periodLifecycleService = mock(PeriodLifecycleService.class);
        CoownershipInboundKafkaConsumer consumer = new CoownershipInboundKafkaConsumer(
                new ObjectMapper().registerModule(new JavaTimeModule()),
                idempotencyService,
                periodLifecycleService
        );

        UUID eventId = UUID.randomUUID();
        UUID rentalListingId = UUID.randomUUID();
        LocalDate startDate = LocalDate.of(2026, 5, 10);
        LocalDate endDate = LocalDate.of(2026, 5, 14);
        BigDecimal totalPrice = new BigDecimal("12000.50");
        String message = """
                {
                  "eventId": "%s",
                  "rentalListingId": "%s",
                  "startDate": "%s",
                  "endDate": "%s",
                  "totalPrice": %s
                }
                """.formatted(eventId, rentalListingId, startDate, endDate, totalPrice);

        doAnswer(invocation -> {
            Runnable action = invocation.getArgument(2);
            action.run();
            return null;
        }).when(idempotencyService).executeOnce(any(UUID.class), anyString(), any(Runnable.class));

        consumer.onBookingConfirmed(message);

        verify(idempotencyService).executeOnce(eq(eventId),
                eq("coownership-booking-confirmed"), any(Runnable.class));
        verify(periodLifecycleService)
                .applyBookingConfirmed(rentalListingId, startDate, endDate, totalPrice);
    }

    @Test
    void onBookingConfirmedRejectsInvalidJsonAndDoesNotCallServices() {
        InboundEventIdempotencyService idempotencyService = mock(InboundEventIdempotencyService.class);
        PeriodLifecycleService periodLifecycleService = mock(PeriodLifecycleService.class);
        CoownershipInboundKafkaConsumer consumer = new CoownershipInboundKafkaConsumer(
                new ObjectMapper().registerModule(new JavaTimeModule()),
                idempotencyService,
                periodLifecycleService
        );

        assertThatCode(() -> consumer.onBookingConfirmed("not-json")).doesNotThrowAnyException();

        verify(idempotencyService, never()).executeOnce(any(UUID.class), anyString(), any(Runnable.class));
        verify(periodLifecycleService,
                never()).applyBookingConfirmed(any(UUID.class),
                any(LocalDate.class),
                any(LocalDate.class),
                any(BigDecimal.class));
    }
}
