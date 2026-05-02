package ru.veshvokrug.coownership.service.outbox;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;
import org.springframework.kafka.core.KafkaTemplate;
import ru.veshvokrug.coownership.model.entity.OutboxMessage;
import ru.veshvokrug.coownership.output.repository.OutboxMessageRepository;

import java.time.Clock;
import java.time.Instant;
import java.time.ZoneOffset;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

class OutboxRelayServiceTest {

    private OutboxMessageRepository outboxMessageRepository;
    private KafkaTemplate<String, String> kafkaTemplate;
    private OutboxRelayService service;

    @BeforeEach
    @SuppressWarnings("unchecked")
    void setUp() {
        outboxMessageRepository = mock(OutboxMessageRepository.class);
        kafkaTemplate = (KafkaTemplate<String, String>) mock(KafkaTemplate.class);
        service = new OutboxRelayService(
                outboxMessageRepository,
                kafkaTemplate,
                new ObjectMapper(),
                Clock.fixed(Instant.parse("2026-04-27T03:00:00Z"), ZoneOffset.UTC),
                "coownership-events",
                100,
                30
        );
    }

    @Test
    void publishNextBatchPublishesEnvelopeAndMarksMessageAsPublished() {
        OutboxMessage message = outboxMessage("SHARE_APPLICATION_CREATED", "{\"foo\":\"bar\"}");
        when(outboxMessageRepository
                .findByPublishedAtIsNullAndNextAttemptAtLessThanEqualOrderByNextAttemptAtAscIdAsc(any(), any()))
                .thenReturn(List.of(message));
        when(kafkaTemplate.send(eq("coownership-events"), eq(message.getId().toString()), any(String.class)))
                .thenReturn(CompletableFuture.completedFuture(null));

        int publishedCount = service.publishNextBatch();

        assertThat(publishedCount).isEqualTo(1);
        ArgumentCaptor<String> envelopeCaptor = ArgumentCaptor.forClass(String.class);
        verify(kafkaTemplate).send(eq("coownership-events"),
                eq(message.getId().toString()), envelopeCaptor.capture());
        assertThat(envelopeCaptor.getValue()).contains("\"eventId\":\"" + message.getId() + "\"");
        assertThat(envelopeCaptor.getValue()).contains("\"eventType\":\"SHARE_APPLICATION_CREATED\"");
        assertThat(envelopeCaptor.getValue()).contains("\"payload\":{\"foo\":\"bar\"}");

        assertThat(message.getPublishedAt()).isEqualTo(Instant.parse("2026-04-27T03:00:00Z"));
        assertThat(message.getLastError()).isNull();
        verify(outboxMessageRepository).save(message);
    }

    @Test
    void publishNextBatchTracksRetryStateWhenKafkaPublishFails() {
        OutboxMessage message = outboxMessage("SLOT_REASSIGNED", "{\"ok\":true}");
        message.setAttemptCount(1);
        when(outboxMessageRepository
                .findByPublishedAtIsNullAndNextAttemptAtLessThanEqualOrderByNextAttemptAtAscIdAsc(any(), any()))
                .thenReturn(List.of(message));
        when(kafkaTemplate.send(eq("coownership-events"),
                eq(message.getId().toString()), any(String.class)))
                .thenReturn(CompletableFuture.failedFuture(new RuntimeException("x".repeat(500))));

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

