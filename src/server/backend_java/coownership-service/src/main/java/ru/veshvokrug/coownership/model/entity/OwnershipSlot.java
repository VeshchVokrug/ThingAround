package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import ru.veshvokrug.coownership.model.OwnershipSlotStatus;
import ru.veshvokrug.coownership.model.baseEntity.BaseEntity;

import java.time.LocalDate;
import java.util.UUID;

/**
 * Один слот = один день одного периода.
 *
 * @author Dmitrii Marchenko 13.04.2026
 */
@Entity
@Table(name = "ownership_slots", uniqueConstraints = {
        @UniqueConstraint(
                name = "uc_slot_period_date",
                columnNames = {"period_id", "date"}
        )
})
public class OwnershipSlot extends BaseEntity {
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(
            name = "period_id",
            nullable = false
    )
    private Period period;

    @Column(name = "owner_id", nullable = false)
    private UUID ownerId;

    @Column(name = "date", nullable = false)
    private LocalDate date;

    @Enumerated(EnumType.STRING)
    @Column(name = "status", nullable = false)
    private OwnershipSlotStatus status = OwnershipSlotStatus.FOR_RENT;

    @Column(name = "is_override", nullable = false)
    private boolean isOverride = false;

    public OwnershipSlot() {
    }

    public Period getPeriod() {
        return period;
    }

    public void setPeriod(Period period) {
        this.period = period;
    }

    public UUID getOwnerId() {
        return ownerId;
    }

    public void setOwnerId(UUID ownerId) {
        this.ownerId = ownerId;
    }

    public LocalDate getDate() {
        return date;
    }

    public void setDate(LocalDate date) {
        this.date = date;
    }

    public OwnershipSlotStatus getStatus() {
        return status;
    }

    public void setStatus(OwnershipSlotStatus status) {
        this.status = status;
    }

    public boolean isOverride() {
        return isOverride;
    }

    public void setOverride(boolean override) {
        isOverride = override;
    }
}
