package ru.veshvokrug.coownership.service.outbox;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.domain.PageRequest;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import ru.veshvokrug.coownership.model.OutboxDestination;
import ru.veshvokrug.coownership.model.entity.OutboxMessage;

import java.util.Comparator;
import ru.veshvokrug.coownership.output.publisher.CatalogCommandsRabbitMqSender;
import ru.veshvokrug.coownership.output.publisher.CoownershipEventsRabbitMqSender;
import ru.veshvokrug.coownership.output.repository.OutboxMessageRepository;

import java.time.Clock;
import java.time.Instant;
import java.util.List;

/**
 * Сервис пакетной публикации outbox-сообщений в RabbitMQ с retry и backoff.
 * Сообщения доставляются по назначению: внутренние события — в topic
 * exchange coownership-events, команды синхронизации листингов — в exchange
 * catalog-service в формате MassTransit.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Service
public class OutboxRelayService {
    private static final Logger log = LoggerFactory.getLogger(OutboxRelayService.class);

    private final OutboxMessageRepository outboxMessageRepository;
    private final CoownershipEventsRabbitMqSender coownershipEventsSender;
    private final CatalogCommandsRabbitMqSender catalogCommandsSender;
    private final Clock clock;
    private final int batchSize;
    private final long retryDelaySeconds;

    public OutboxRelayService(OutboxMessageRepository outboxMessageRepository,
                              CoownershipEventsRabbitMqSender coownershipEventsSender,
                              CatalogCommandsRabbitMqSender catalogCommandsSender,
                              Clock clock,
                              @Value("${coownership.outbox.batch-size:100}") int batchSize,
                              @Value("${coownership.outbox.retry-delay-seconds:30}") long retryDelaySeconds) {
        this.outboxMessageRepository = outboxMessageRepository;
        this.coownershipEventsSender = coownershipEventsSender;
        this.catalogCommandsSender = catalogCommandsSender;
        this.clock = clock;
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

        // Сортируем пачку: CREATE-команды в каталог идут раньше UPDATE,
        // чтобы каталог успел создать запись до получения UPDATE
        List<OutboxMessage> sorted = batch.stream()
                .sorted(Comparator.comparingInt(msg -> {
                    if (msg.getDestination() == OutboxDestination.CATALOG_RABBITMQ) {
                        return msg.getEventType().endsWith("CREATE") ? 0 : 1;
                    }
                    return 2;
                }))
                .toList();

        int publishedCount = 0;
        for (OutboxMessage message : sorted) {
            if (tryPublish(message, now)) {
                publishedCount++;
            }
        }
        return publishedCount;
    }

    private boolean tryPublish(OutboxMessage message, Instant now) {
        try {
            deliver(message);
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

    /**
     * Доставляет сообщение в RabbitMQ согласно его назначению.
     */
    private void deliver(OutboxMessage message) {
        switch (message.getDestination()) {
            case CATALOG_RABBITMQ -> catalogCommandsSender.send(message.getId(), message.getPayload());
            case COOWNERSHIP_EVENTS -> coownershipEventsSender.send(
                    message.getId(), message.getEventType(), message.getPayload());
        }
    }

    private String trimError(String error) {
        if (error == null || error.isBlank()) {
            return "Unknown outbox error";
        }
        return error.length() > 255 ? error.substring(0, 255) : error;
    }
}
