package ru.veshvokrug.coownership.service.outbox;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.domain.PageRequest;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import ru.veshvokrug.coownership.model.entity.OutboxMessage;
import ru.veshvokrug.coownership.output.repository.OutboxMessageRepository;

import java.time.Clock;
import java.time.Instant;
import java.util.List;

/**
 * Сервис пакетной публикации outbox-сообщений в Kafka с retry и backoff.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Service
public class OutboxRelayService {
    private static final Logger log = LoggerFactory.getLogger(OutboxRelayService.class);

    private final OutboxMessageRepository outboxMessageRepository;
    private final KafkaTemplate<String, String> kafkaTemplate;
    private final ObjectMapper objectMapper;
    private final Clock clock;
    private final String topic;
    private final int batchSize;
    private final long retryDelaySeconds;

    public OutboxRelayService(OutboxMessageRepository outboxMessageRepository,
                              KafkaTemplate<String, String> kafkaTemplate,
                              ObjectMapper objectMapper,
                              Clock clock,
                              @Value("${coownership.kafka.outbox.topic:coownership-events}") String topic,
                              @Value("${coownership.outbox.batch-size:100}") int batchSize,
                              @Value("${coownership.outbox.retry-delay-seconds:30}") long retryDelaySeconds) {
        this.outboxMessageRepository = outboxMessageRepository;
        this.kafkaTemplate = kafkaTemplate;
        this.objectMapper = objectMapper;
        this.clock = clock;
        this.topic = topic;
        this.batchSize = batchSize;
        this.retryDelaySeconds = retryDelaySeconds;
    }

    @Transactional
    public int publishNextBatch() {
        Instant now = Instant.now(clock);
        List<OutboxMessage> batch = outboxMessageRepository
                .findByPublishedAtIsNullAndNextAttemptAtLessThanEqualOrderByNextAttemptAtAscIdAsc(
                        now,
                        PageRequest.of(0, batchSize)
                );

        int publishedCount = 0;
        for (OutboxMessage message : batch) {
            if (tryPublish(message, now)) {
                publishedCount++;
            }
        }
        return publishedCount;
    }

    private boolean tryPublish(OutboxMessage message, Instant now) {
        try {
            String envelope = buildEnvelope(message);
            kafkaTemplate.send(topic, message.getId().toString(), envelope).get();
            message.setPublishedAt(now);
            message.setLastError(null);
            outboxMessageRepository.save(message);
            return true;
        } catch (Exception ex) {
            message.setAttemptCount(message.getAttemptCount() + 1);
            message.setLastError(trimError(ex.getMessage()));
            message.setNextAttemptAt(now.plusSeconds(retryDelaySeconds));
            outboxMessageRepository.save(message);
            log.warn("Outbox publish failed: id={}, eventType={}, error={}",
                    message.getId(), message.getEventType(), message.getLastError());
            return false;
        }
    }

    private String buildEnvelope(OutboxMessage message) throws Exception {
        ObjectNode root = objectMapper.createObjectNode();
        root.put("eventId", message.getId().toString());
        root.put("eventType", message.getEventType());

        JsonNode payloadNode = objectMapper.readTree(message.getPayload());
        root.set("payload", payloadNode);

        return objectMapper.writeValueAsString(root);
    }

    private String trimError(String error) {
        if (error == null || error.isBlank()) {
            return "Unknown outbox error";
        }
        return error.length() > 255 ? error.substring(0, 255) : error;
    }
}
