package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import jakarta.validation.constraints.DecimalMin;
import ru.veshvokrug.coownership.model.PeriodStatus;
import ru.veshvokrug.coownership.model.baseEntity.AuditableEntity;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.UUID;

/**
 * @author Dmitrii Marchenko 13.04.2026
 */
@Entity
@Table(name = "periods")
public class Period extends AuditableEntity {
    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(
            name = "coownership_listing_id",
            nullable = false)
    private CoownershipListing coownershipListing;

    @Column(name = "rental_listing_id")
    private UUID rentalListingId;

    @Column(name = "start_date")
    private Instant startDate;

    @Column(name = "end_date")
    private Instant endDate;

    @Column(
            name = "total_income",
            nullable = false,
            precision = 19,
            scale = 2)
    @DecimalMin("0.00")
    private BigDecimal totalIncome = BigDecimal.ZERO;

    @Column(name = "status")
    private PeriodStatus status = PeriodStatus.ACTIVE;
}
