package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;

import java.time.Instant;
import java.util.UUID;

/**
 * @author Dmitrii Marchenko 14.04.2026
 */
@Entity
@Table(name = "processed_events")
public class ProcessedEvent {
    @Id
    private UUID eventId;

    @Column(nullable = false)
    private Instant processedAt;
}
