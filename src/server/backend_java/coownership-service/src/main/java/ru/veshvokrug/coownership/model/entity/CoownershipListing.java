package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.baseEntity.AuditableEntity;

import java.time.LocalDate;
import java.util.UUID;

/**
 * Модель для хранения списка совладения
 *
 * @author Dmitrii Marchenko 13.04.2026
 */
@Entity
@Table(name = "coownership_listings")
public class CoownershipListing extends AuditableEntity {
    // ссылка на запись в Catalog, не полноценный FK
    @Column(name = "catalog_listing_id")
    private UUID catalogListingId;

    @Column(name = "owner_id", nullable = false)
    private UUID ownerId;

    @Column(name = "total_shares", nullable = false)
    private int totalShares;

    @Column(name = "filled_shares", nullable = false)
    private int filledShares = 0;

    @Enumerated(EnumType.STRING)
    @Column(name = "status", nullable = false)
    private CoownershipStatus status = CoownershipStatus.OPEN;

    @Column(name = "funding_deadline", nullable = false)
    private LocalDate fundingDeadline;

    @Column(name = "version")
    @Version
    private long version = 0;
}
