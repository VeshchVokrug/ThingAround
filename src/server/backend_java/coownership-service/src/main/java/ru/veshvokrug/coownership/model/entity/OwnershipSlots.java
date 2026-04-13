package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import ru.veshvokrug.coownership.model.OwnershipSlotStatus;
import ru.veshvokrug.coownership.model.baseEntity.BaseEntity;

import java.time.Instant;
import java.util.UUID;

/**
 * Модель для зранения информации о каждом дне
 *
 * @author Dmitrii Marchenko 13.04.2026
 */
@Entity
@Table(name = "ownership_slots")
public class OwnershipSlots extends BaseEntity {
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(
            name = "period_id",
            nullable = false
    )
    private Period period;

    @Column(name = "owner_id", nullable = false)
    private UUID ownerId;

    @Column(name = "date", nullable = false)
    private Instant date;

    @Column(name = "status", nullable = false)
    private OwnershipSlotStatus status = OwnershipSlotStatus.FOR_RENT;

    @Column(name = "is_override", nullable = false)
    private boolean isOverride = false;
}
