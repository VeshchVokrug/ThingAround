package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Table;
import jakarta.persistence.UniqueConstraint;
import ru.veshvokrug.coownership.model.baseEntity.BaseEntity;

import java.time.Instant;
import java.util.UUID;

/**
 * Идемпотентность входящих событий из RabbitMQ
 *
 * @author Dmitrii Marchenko 14.04.2026
 */
@Entity
@Table(name = "processed_events", uniqueConstraints = {
        @UniqueConstraint(columnNames = {"event_id", "consumer_name"})
})
public class ProcessedEvent extends BaseEntity {
    @Column(name = "event_id", nullable = false)
    private UUID eventId;

    @Column(name = "consumer_name", nullable = false)
    private String consumerName;

    @Column(name = "processed_at", nullable = false)
    private Instant processedAt;

    public ProcessedEvent() {
    }

    public UUID getEventId() {
        return eventId;
    }

    public void setEventId(UUID eventId) {
        this.eventId = eventId;
    }

    public String getConsumerName() {
        return consumerName;
    }

    public void setConsumerName(String consumerName) {
        this.consumerName = consumerName;
    }

    public Instant getProcessedAt() {
        return processedAt;
    }

    public void setProcessedAt(Instant processedAt) {
        this.processedAt = processedAt;
    }
}
