package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Table;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;
import ru.veshvokrug.coownership.model.baseEntity.BaseEntity;

import java.time.Instant;

/**
 * @author Dmitrii Marchenko 14.04.2026
 */
@Entity
@Table(name = "outbox")
public class OutboxMessage extends BaseEntity {

    @Column(name = "event_type", nullable = false)
    private String eventType;

    @JdbcTypeCode(SqlTypes.JSON)
    @Column(nullable = false, columnDefinition = "jsonb")
    private String payload;

    @Column
    private Instant publishedAt;
}
