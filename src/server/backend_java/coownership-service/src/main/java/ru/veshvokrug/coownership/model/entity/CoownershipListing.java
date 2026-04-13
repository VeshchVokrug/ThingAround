package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.baseEntity.AuditableEntity;

import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
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
    private int catalogListingId;

    @Column(name = "owner_id", nullable = false)
    private UUID ownerId;

    //todo что то добавить сюда

    @Column(name = "total_shares", nullable = false)
    private int totalShares;

    @Column(name = "filled_shares", nullable = false)
    private int filledShares = 0;

    @OneToMany(
            mappedBy = "coownershipListing",
            cascade = CascadeType.ALL,
            orphanRemoval = true)
    private List<OwnershipShare> ownershipShares = new ArrayList<>();

    @OneToMany(
            mappedBy = "coownershipListing",
            cascade = CascadeType.ALL,
            orphanRemoval = true)
    private List<Period> periods = new ArrayList<>();

    @Column(name = "status",nullable = false)
    private CoownershipStatus status = CoownershipStatus.OPEN;

    @Column(name = "funding_deadline")
    private Instant fundingDeadline;

    @Column(name = "version")
    @Version
    private long version = 0;
}
