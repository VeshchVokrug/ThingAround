package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.Index;
import jakarta.persistence.Table;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;
import ru.veshvokrug.coownership.model.OutboxDestination;
import ru.veshvokrug.coownership.model.baseEntity.BaseEntity;

import java.time.Instant;

/**
 * Исходящие сообщения RabbitMQ, публикуемые через outbox
 *
 * @author Dmitrii Marchenko 14.04.2026
 */
@Entity
@Table(name = "outbox", indexes = {
        @Index(name = "idx_outbox_unpublished",
                columnList = "published_at, next_attempt_at")
})
public class OutboxMessage extends BaseEntity {
    @Column(name = "event_type", nullable = false)
    private String eventType;

    @Enumerated(EnumType.STRING)
    @Column(name = "destination", nullable = false)
    private OutboxDestination destination = OutboxDestination.COOWNERSHIP_EVENTS;

    @JdbcTypeCode(SqlTypes.JSON)
    @Column(nullable = false, columnDefinition = "jsonb")
    private String payload;

    @Column(name = "published_at")
    private Instant publishedAt;

    @Column(name = "attempt_count", nullable = false)
    private int attemptCount = 0;

    @Column(name = "next_attempt_at", nullable = false)
    private Instant nextAttemptAt = Instant.now();

    @Column(name = "last_error")
    private String lastError;

    public OutboxMessage() {
    }

    public OutboxDestination getDestination() {
        return destination;
    }

    public void setDestination(OutboxDestination destination) {
        this.destination = destination;
    }

    public String getEventType() {
        return eventType;
    }

    public void setEventType(String eventType) {
        this.eventType = eventType;
    }

    public String getPayload() {
        return payload;
    }

    public void setPayload(String payload) {
        this.payload = payload;
    }

    public Instant getPublishedAt() {
        return publishedAt;
    }

    public void setPublishedAt(Instant publishedAt) {
        this.publishedAt = publishedAt;
    }

    public int getAttemptCount() {
        return attemptCount;
    }

    public void setAttemptCount(int attemptCount) {
        this.attemptCount = attemptCount;
    }

    public Instant getNextAttemptAt() {
        return nextAttemptAt;
    }

    public void setNextAttemptAt(Instant nextAttemptAt) {
        this.nextAttemptAt = nextAttemptAt;
    }

    public String getLastError() {
        return lastError;
    }

    public void setLastError(String lastError) {
        this.lastError = lastError;
    }
}
