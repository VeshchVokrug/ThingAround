package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import jakarta.validation.constraints.DecimalMin;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.baseEntity.AuditableEntity;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;
import java.util.UUID;

/**
 * Хранит состояние набора совладельцев
 *
 * @author Dmitrii Marchenko 13.04.2026
 */
@Entity
@Table(name = "coownership_listings")
public class CoownershipListing extends AuditableEntity {
    @Column(
            name = "price",
            nullable = false,
            precision = 19,
            scale = 2)
    @DecimalMin("0.00")
    private BigDecimal price;

    // ссылка на запись в Catalog, не полноценный FK
    @Column(name = "catalog_listing_id", nullable = false)
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

    // ---- Карточка для catalog-service ----
    // Хранится локально, чтобы сервис мог публиковать полное состояние
    // листинга (контракт CoownershipListingMessage) при любом изменении.

    @Column(name = "title", nullable = false)
    private String title = "";

    @Column(name = "description", nullable = false)
    private String description = "";

    @Column(name = "category_slug", nullable = false)
    private String categorySlug = "";

    @Column(name = "city", nullable = false)
    private String city = "";

    @JdbcTypeCode(SqlTypes.JSON)
    @Column(name = "images_urls", columnDefinition = "jsonb")
    private List<String> imagesUrls;

    public CoownershipListing() {
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

    public String getTitle() {
        return title;
    }

    public void setTitle(String title) {
        this.title = title;
    }

    public String getDescription() {
        return description;
    }

    public void setDescription(String description) {
        this.description = description;
    }

    public String getCategorySlug() {
        return categorySlug;
    }

    public void setCategorySlug(String categorySlug) {
        this.categorySlug = categorySlug;
    }

    public String getCity() {
        return city;
    }

    public void setCity(String city) {
        this.city = city;
    }

    public List<String> getImagesUrls() {
        return imagesUrls;
    }

    public void setImagesUrls(List<String> imagesUrls) {
        this.imagesUrls = imagesUrls;
    }
}
