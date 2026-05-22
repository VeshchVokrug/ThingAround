package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.baseEntity.AuditableEntity;

import java.time.Instant;
import java.util.UUID;

/**
 * Уведомление для polling по заявкам на доли.
 * Хранится отдельно от заявок, чтобы frontend мог опрашивать легкую read-модель
 * и не читать всю бизнес-сущность каждый раз.
 *
 * @author Dmitrii Marchenko 25.04.2026
 */
@Entity
@Table(name = "share_application_notifications", indexes = {
        @Index(name = "idx_share_application_notifications_recipient_expires",
                columnList = "recipient_id, expires_at")
})
public class ShareApplicationNotification extends AuditableEntity {
    @Column(name = "recipient_id", nullable = false)
    private UUID recipientId;

    @Column(name = "application_id", nullable = false)
    private UUID applicationId;

    @Column(name = "listing_id", nullable = false)
    private UUID listingId;

    @Column(name = "owner_id", nullable = false)
    private UUID ownerId;

    @Column(name = "applicant_id", nullable = false)
    private UUID applicantId;

    @Column(name = "shares_count", nullable = false)
    private int sharesCount;

    @Enumerated(EnumType.STRING)
    @Column(name = "application_status", nullable = false)
    private ShareApplicationStatus applicationStatus;

    @Column(name = "event_type", nullable = false, length = 64)
    private String eventType;

    @Column(name = "expires_at", nullable = false)
    private Instant expiresAt;

    public ShareApplicationNotification() {
    }

    public UUID getRecipientId() {
        return recipientId;
    }

    public void setRecipientId(UUID recipientId) {
        this.recipientId = recipientId;
    }

    public UUID getApplicationId() {
        return applicationId;
    }

    public void setApplicationId(UUID applicationId) {
        this.applicationId = applicationId;
    }

    public UUID getListingId() {
        return listingId;
    }

    public void setListingId(UUID listingId) {
        this.listingId = listingId;
    }

    public UUID getOwnerId() {
        return ownerId;
    }

    public void setOwnerId(UUID ownerId) {
        this.ownerId = ownerId;
    }

    public UUID getApplicantId() {
        return applicantId;
    }

    public void setApplicantId(UUID applicantId) {
        this.applicantId = applicantId;
    }

    public int getSharesCount() {
        return sharesCount;
    }

    public void setSharesCount(int sharesCount) {
        this.sharesCount = sharesCount;
    }

    public ShareApplicationStatus getApplicationStatus() {
        return applicationStatus;
    }

    public void setApplicationStatus(ShareApplicationStatus applicationStatus) {
        this.applicationStatus = applicationStatus;
    }

    public String getEventType() {
        return eventType;
    }

    public void setEventType(String eventType) {
        this.eventType = eventType;
    }

    public Instant getExpiresAt() {
        return expiresAt;
    }

    public void setExpiresAt(Instant expiresAt) {
        this.expiresAt = expiresAt;
    }
}
