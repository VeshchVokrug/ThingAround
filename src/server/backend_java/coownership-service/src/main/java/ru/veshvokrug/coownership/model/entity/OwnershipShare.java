package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import ru.veshvokrug.coownership.model.baseEntity.BaseEntity;

import java.util.UUID;

/**
 * Модель для хранения частей совладения с шаблоном дней
 *
 * @author Dmitrii Marchenko 13.04.2026
 */
@Entity
@Table(name = "ownership_shares", indexes = {
        @Index(name = "idx_ownershipshare", columnList = "coownership_listing_id")
})
public class OwnershipShare extends BaseEntity {
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(
            name = "coownership_listing_id",
            nullable = false)
    private CoownershipListing coownershipListing;

    @Column(name = "owner_id")
    private UUID ownerId;

    @Column(name = "percentage", nullable = false)
    @Min(value = 1, message = "Доля должна быть больше 1")
    @Max(value = 99, message = "Доля должна быть меньше 99")
    private int percentage;

    @Column(name = "template_days_mask", nullable = false)
    private int templateDaysMask;

    @Column(name = "is_locked", nullable = false)
    private boolean isLocked = false;
}
