package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.Size;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.baseEntity.AuditableEntity;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.UUID;

/**
 * Хранит состояние набора совладельцев
 *
 * @author Dmitrii Marchenko 13.04.2026
 */
@Entity
@Table(name = "coownership_listings")
public class CoownershipListing extends AuditableEntity {
    @Column(name = "name", nullable = false, length = 128)
    @Size(
            min = 8,
            max = 128,
            message = "Имя не может быть больше 128 символов")
    private String name;

    @Column(
            name = "description", length = 1024)
    @Size(
            max = 1024,
            message = "Описание не может быть больше 1024 символов")
    private String description;

    @Column(
            name = "price",
            nullable = false,
            precision = 19,
            scale = 2)
    @DecimalMin("0.00")
    private BigDecimal price;

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

    @Column(name = "version", nullable = false)
    @Version
    private long version = 0;

    public CoownershipListing() {
    }

    public String getName() {
        return name;
    }

    public void setName(String name) {
        this.name = name;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }

    public BigDecimal getPrice() {
        return price;
    }

    public void setPrice(BigDecimal price) {
        this.price = price;
    }

    public UUID getCatalogListingId() {
        return catalogListingId;
    }

    public void setCatalogListingId(UUID catalogListingId) {
        this.catalogListingId = catalogListingId;
    }

    public UUID getOwnerId() {
        return ownerId;
    }

    public void setOwnerId(UUID ownerId) {
        this.ownerId = ownerId;
    }

    public int getTotalShares() {
        return totalShares;
    }

    public void setTotalShares(int totalShares) {
        this.totalShares = totalShares;
    }

    public int getFilledShares() {
        return filledShares;
    }

    public void setFilledShares(int filledShares) {
        this.filledShares = filledShares;
    }

    public CoownershipStatus getStatus() {
        return status;
    }

    public void setStatus(CoownershipStatus status) {
        this.status = status;
    }

    public LocalDate getFundingDeadline() {
        return fundingDeadline;
    }

    public void setFundingDeadline(LocalDate fundingDeadline) {
        this.fundingDeadline = fundingDeadline;
    }

    public long getVersion() {
        return version;
    }

    public void setVersion(long version) {
        this.version = version;
    }
}
