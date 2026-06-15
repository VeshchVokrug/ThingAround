package ru.veshvokrug.coownership.service.outbox;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import ru.veshvokrug.coownership.model.OutboxDestination;
import ru.veshvokrug.coownership.model.entity.OutboxMessage;
import ru.veshvokrug.coownership.output.publisher.CatalogCommandsRabbitMqSender;
import ru.veshvokrug.coownership.output.publisher.CoownershipEventsRabbitMqSender;
import ru.veshvokrug.coownership.output.repository.OutboxMessageRepository;

import java.time.Clock;
import java.time.Instant;
import java.time.ZoneOffset;
import java.util.List;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.doThrow;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoInteractions;
import static org.mockito.Mockito.when;

class OutboxRelayServiceTest {

    private OutboxMessageRepository outboxMessageRepository;
    private CoownershipEventsRabbitMqSender coownershipEventsSender;
    private CatalogCommandsRabbitMqSender catalogCommandsSender;
    private OutboxRelayService service;

    @BeforeEach
    void setUp() {
        outboxMessageRepository = mock(OutboxMessageRepository.class);
        coownershipEventsSender = mock(CoownershipEventsRabbitMqSender.class);
        catalogCommandsSender = mock(CatalogCommandsRabbitMqSender.class);
        service = new OutboxRelayService(
                outboxMessageRepository,
                coownershipEventsSender,
                catalogCommandsSender,
                Clock.fixed(Instant.parse("2026-04-27T03:00:00Z"), ZoneOffset.UTC),
                100,
                30
        );
    }

    @Test
    void publishNextBatchSendsDomainEventAndMarksMessageAsPublished() {
        OutboxMessage message = outboxMessage("SHARE_APPLICATION_CREATED", "{\"foo\":\"bar\"}");
        when(outboxMessageRepository
                .findByPublishedAtIsNullAndNextAttemptAtLessThanEqualOrderByNextAttemptAtAscIdAsc(any(), any()))
                .thenReturn(List.of(message));

        int publishedCount = service.publishNextBatch();

        assertThat(publishedCount).isEqualTo(1);
        verify(coownershipEventsSender).send(
                message.getId(), "SHARE_APPLICATION_CREATED", "{\"foo\":\"bar\"}");
        verifyNoInteractions(catalogCommandsSender);

        assertThat(message.getPublishedAt()).isEqualTo(Instant.parse("2026-04-27T03:00:00Z"));
        assertThat(message.getLastError()).isNull();
        verify(outboxMessageRepository).save(message);
    }

    @Test
    void publishNextBatchRoutesCatalogMessagesToCatalogSender() {
        OutboxMessage message = outboxMessage("CATALOG_LISTING_CREATE", "{\"title\":\"x\"}");
        message.setDestination(OutboxDestination.CATALOG_RABBITMQ);
        when(outboxMessageRepository
                .findByPublishedAtIsNullAndNextAttemptAtLessThanEqualOrderByNextAttemptAtAscIdAsc(any(), any()))
                .thenReturn(List.of(message));

        int publishedCount = service.publishNextBatch();

        assertThat(publishedCount).isEqualTo(1);
        verify(catalogCommandsSender).send(message.getId(), "{\"title\":\"x\"}");
        verifyNoInteractions(coownershipEventsSender);
        assertThat(message.getPublishedAt()).isNotNull();
    }

    @Test
    void publishNextBatchTracksRetryStateWhenPublishFails() {
        OutboxMessage message = outboxMessage("SLOT_REASSIGNED", "{\"ok\":true}");
        message.setAttemptCount(1);
        when(outboxMessageRepository
                .findByPublishedAtIsNullAndNextAttemptAtLessThanEqualOrderByNextAttemptAtAscIdAsc(any(), any()))
                .thenReturn(List.of(message));
        doThrow(new IllegalStateException("x".repeat(500)))
                .when(coownershipEventsSender).send(any(UUID.class), anyString(), anyString());

        int publishedCount = service.publishNextBatch();

        assertThat(publishedCount).isZero();
        assertThat(message.getPublishedAt()).isNull();
        assertThat(message.getAttemptCount()).isEqualTo(2);
        assertThat(message.getNextAttemptAt()).isEqualTo(Instant.parse("2026-04-27T03:00:30Z"));
        assertThat(message.getLastError()).hasSizeLessThanOrEqualTo(255);
        verify(outboxMessageRepository).save(message);
    }

    private OutboxMessage outboxMessage(String type, String payload) {
        OutboxMessage message = new OutboxMessage();
        message.setId(UUID.randomUUID());
        message.setEventType(type);
        message.setPayload(payload);
        message.setNextAttemptAt(Instant.parse("2026-04-27T02:59:00Z"));
        return message;
    }
}
