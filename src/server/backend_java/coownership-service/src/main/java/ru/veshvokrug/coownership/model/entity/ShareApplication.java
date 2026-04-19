package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.baseEntity.BaseEntity;

import java.util.UUID;

/**
 * Заявки пользователей на занятие доли.
 *
 * @author Dmitrii Marchenko 14.04.2026
 */
@Entity
@Table(name = "share_applications", uniqueConstraints = {
        @UniqueConstraint(name = "uc_shareapplication",
                columnNames = {"coownership_listing_id", "applicant_id"})
})
public class ShareApplication extends BaseEntity {
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(
            name = "coownership_listing_id",
            nullable = false
    )
    private CoownershipListing listing;

    @Column(name = "applicant_id", nullable = false)
    private UUID applicantId;

    @Enumerated(EnumType.STRING)
    @Column(name = "status", nullable = false)
    private ShareApplicationStatus status = ShareApplicationStatus.PENDING;

    public ShareApplication() {
    }

    public CoownershipListing getListing() {
        return listing;
    }

    public void setListing(CoownershipListing listing) {
        this.listing = listing;
    }

    public UUID getApplicantId() {
        return applicantId;
    }

    public void setApplicantId(UUID applicantId) {
        this.applicantId = applicantId;
    }

    public ShareApplicationStatus getStatus() {
        return status;
    }

    public void setStatus(ShareApplicationStatus status) {
        this.status = status;
    }
}
