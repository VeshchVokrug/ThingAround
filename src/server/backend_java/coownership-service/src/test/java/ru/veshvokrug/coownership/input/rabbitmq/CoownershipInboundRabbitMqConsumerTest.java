package ru.veshvokrug.coownership.input.rabbitmq;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import org.junit.jupiter.api.Test;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessageProperties;
import ru.veshvokrug.coownership.service.InboundEventIdempotencyService;
import ru.veshvokrug.coownership.service.PeriodLifecycleService;

import java.nio.charset.StandardCharsets;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThatCode;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

/**
 * Тесты для {@link CoownershipInboundRabbitMqConsumer}.
 * Все сообщения приходят в формате MassTransit-конверта.
 */
class CoownershipInboundRabbitMqConsumerTest {

    private final ObjectMapper objectMapper = new ObjectMapper().registerModule(new JavaTimeModule());

    @Test
    void onRentalEventLinksRentalListingWhenBookingRequested() {
        InboundEventIdempotencyService idempotency = mock(InboundEventIdempotencyService.class);
        PeriodLifecycleService lifecycle = mock(PeriodLifecycleService.class);
        CoownershipInboundRabbitMqConsumer consumer =
                new CoownershipInboundRabbitMqConsumer(objectMapper, idempotency, lifecycle);

        UUID bookingId = UUID.randomUUID();
        UUID listingId = UUID.randomUUID();

        String envelope = """
                {
                  "messageId": "%s",
                  "messageType": [
                    "urn:message:Core.SAGA.Contracts.Events:RentalBookingRequestedEvent",
                    "urn:message:Core.SAGA.Contracts.Events:IRentalEvents"
                  ],
                  "message": {
                    "BookingId": "%s",
                    "ListingId": "%s",
                    "TenantId": "%s",
                    "OwnerId": "%s",
                    "StartDate": "2026-06-20",
                    "EndDate": "2026-06-22",
                    "ExpectedPrice": 4500.00
                  },
                  "sentTime": "2026-06-12T10:00:00Z"
                }
                """.formatted(UUID.randomUUID(), bookingId, listingId,
                              UUID.randomUUID(), UUID.randomUUID());

        doAnswer(inv -> { ((Runnable) inv.getArgument(2)).run(); return null; })
                .when(idempotency).executeOnce(any(UUID.class), anyString(), any(Runnable.class));

        consumer.onRentalEvent(amqpMessage(envelope));

        verify(idempotency).executeOnce(eq(bookingId), eq("coownership-rental-listing-created"),
                any(Runnable.class));
        verify(lifecycle).linkRentalListingByCatalogId(listingId, bookingId);
    }

    @Test
    void onRentalEventAppliesBookingWhenApproved() {
        InboundEventIdempotencyService idempotency = mock(InboundEventIdempotencyService.class);
        PeriodLifecycleService lifecycle = mock(PeriodLifecycleService.class);
        CoownershipInboundRabbitMqConsumer consumer =
                new CoownershipInboundRabbitMqConsumer(objectMapper, idempotency, lifecycle);

        UUID bookingId = UUID.randomUUID();

        String envelope = """
                {
                  "messageId": "%s",
                  "messageType": [
                    "urn:message:Core.SAGA.Contracts.Events:RentalBookingApprovedEvent",
                    "urn:message:Core.SAGA.Contracts.Events:IRentalEvents"
                  ],
                  "message": {
                    "BookingId": "%s",
                    "OwnerId": "%s"
                  },
                  "sentTime": "2026-06-12T10:00:00Z"
                }
                """.formatted(UUID.randomUUID(), bookingId, UUID.randomUUID());

        doAnswer(inv -> { ((Runnable) inv.getArgument(2)).run(); return null; })
                .when(idempotency).executeOnce(any(UUID.class), anyString(), any(Runnable.class));

        consumer.onRentalEvent(amqpMessage(envelope));

        verify(idempotency).executeOnce(eq(bookingId), eq("coownership-booking-confirmed"),
                any(Runnable.class));
        verify(lifecycle).applyBookingApproved(bookingId);
    }

    @Test
    void onRentalEventSkipsUnknownEventType() {
        InboundEventIdempotencyService idempotency = mock(InboundEventIdempotencyService.class);
        PeriodLifecycleService lifecycle = mock(PeriodLifecycleService.class);
        CoownershipInboundRabbitMqConsumer consumer =
                new CoownershipInboundRabbitMqConsumer(objectMapper, idempotency, lifecycle);

        String envelope = """
                {
                  "messageId": "%s",
                  "messageType": [
                    "urn:message:Core.SAGA.Contracts.Events:RentalBookingCancelledEvent",
                    "urn:message:Core.SAGA.Contracts.Events:IRentalEvents"
                  ],
                  "message": { "BookingId": "%s", "TenantId": "%s", "Reason": "user cancelled" }
                }
                """.formatted(UUID.randomUUID(), UUID.randomUUID(), UUID.randomUUID());

        consumer.onRentalEvent(amqpMessage(envelope));

        verifyNoInteractions(idempotency, lifecycle);
    }

    @Test
    void onRentalEventSilentlySkipsMalformedJson() {
        CoownershipInboundRabbitMqConsumer consumer =
                new CoownershipInboundRabbitMqConsumer(objectMapper,
                        mock(InboundEventIdempotencyService.class),
                        mock(PeriodLifecycleService.class));

        assertThatCode(() -> consumer.onRentalEvent(amqpMessage("not-json")))
                .doesNotThrowAnyException();
    }

    private Message amqpMessage(String body) {
        return new Message(body.getBytes(StandardCharsets.UTF_8), new MessageProperties());
    }
}
