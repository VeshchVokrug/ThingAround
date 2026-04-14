package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import ru.veshvokrug.coownership.model.OwnershipSlotStatus;
import ru.veshvokrug.coownership.model.baseEntity.BaseEntity;

import java.time.LocalDate;
import java.util.UUID;

/**
 * Модель для зранения информации о каждом дне
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
}
