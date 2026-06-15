package ru.veshvokrug.coownership.model.entity;

import jakarta.persistence.*;
import jakarta.validation.constraints.DecimalMin;
import ru.veshvokrug.coownership.model.PeriodStatus;
import ru.veshvokrug.coownership.model.baseEntity.AuditableEntity;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.UUID;

/**
 * Расчётный период (календарный месяц)
 *
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

    @Column(name = "start_date", nullable = false)
    private LocalDate startDate;

    @Column(name = "end_date", nullable = false)
    private LocalDate endDate;

    @Column(
            name = "total_income",
            nullable = false,
            precision = 19,
            scale = 2)
    @DecimalMin("0.00")
    private BigDecimal totalIncome = BigDecimal.ZERO;

    @Enumerated(EnumType.STRING)
    @Column(name = "status", nullable = false)
    private PeriodStatus status = PeriodStatus.ACTIVE;

    @Column(name = "pending_booking_id")
    private UUID pendingBookingId;

    @Column(name = "pending_booking_price", precision = 19, scale = 2)
    private BigDecimal pendingBookingPrice;

    public Period() {
    }

    public CoownershipListing getCoownershipListing() {
        return coownershipListing;
    }

    public void setCoownershipListing(CoownershipListing coownershipListing) {
        this.coownershipListing = coownershipListing;
    }

    public UUID getRentalListingId() {
        return rentalListingId;
    }

    public void setRentalListingId(UUID rentalListingId) {
        this.rentalListingId = rentalListingId;
    }

    public LocalDate getStartDate() {
        return startDate;
    }

    public void setStartDate(LocalDate startDate) {
        this.startDate = startDate;
    }

    public LocalDate getEndDate() {
        return endDate;
    }

    public void setEndDate(LocalDate endDate) {
        this.endDate = endDate;
    }

    public BigDecimal getTotalIncome() {
        return totalIncome;
    }

    public void setTotalIncome(BigDecimal totalIncome) {
        this.totalIncome = totalIncome;
    }

    public UUID getPendingBookingId() {
        return pendingBookingId;
    }

    public void setPendingBookingId(UUID pendingBookingId) {
        this.pendingBookingId = pendingBookingId;
    }

    public BigDecimal getPendingBookingPrice() {
        return pendingBookingPrice;
    }

    public void setPendingBookingPrice(BigDecimal pendingBookingPrice) {
        this.pendingBookingPrice = pendingBookingPrice;
    }

    public PeriodStatus getStatus() {
        return status;
    }

    public void setStatus(PeriodStatus status) {
        this.status = status;
    }
}
