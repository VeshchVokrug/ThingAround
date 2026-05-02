package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;
import ru.veshvokrug.coownership.model.OwnershipSlotStatus;
import ru.veshvokrug.coownership.model.PeriodStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.OwnershipShare;
import ru.veshvokrug.coownership.model.entity.OwnershipSlot;
import ru.veshvokrug.coownership.model.entity.Period;
import ru.veshvokrug.coownership.output.repository.OwnershipShareRepository;
import ru.veshvokrug.coownership.output.repository.OwnershipSlotsRepository;
import ru.veshvokrug.coownership.output.repository.PeriodRepository;

import java.math.BigDecimal;
import java.time.Clock;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

class PeriodLifecycleServiceTest {

    private PeriodRepository periodRepository;
    private OwnershipSlotsRepository ownershipSlotsRepository;
    private OwnershipShareRepository ownershipShareRepository;
    private OutboxEventService outboxEventService;
    private PeriodLifecycleService service;

    @BeforeEach
    void setUp() {
        periodRepository = mock(PeriodRepository.class);
        ownershipSlotsRepository = mock(OwnershipSlotsRepository.class);
        ownershipShareRepository = mock(OwnershipShareRepository.class);
        outboxEventService = mock(OutboxEventService.class);

        service = new PeriodLifecycleService(
                periodRepository,
                ownershipSlotsRepository,
                ownershipShareRepository,
                outboxEventService,
                new SettlementCalculator(),
                Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC)
        );
    }

    @Test
    void triggerFilledOutCreatesActivePeriodSlotsAndOutboxEvent() {
        UUID listingId = UUID.randomUUID();
        CoownershipListing listing = new CoownershipListing();
        listing.setId(listingId);
        listing.setCatalogListingId(UUID.randomUUID());
        listing.setOwnerId(UUID.randomUUID());

        OwnershipShare first = new OwnershipShare();
        first.setId(UUID.randomUUID());
        first.setOwnerId(UUID.randomUUID());
        OwnershipShare second = new OwnershipShare();
        second.setId(UUID.randomUUID());
        second.setOwnerId(UUID.randomUUID());

        when(periodRepository
                .findByCoownershipListing_IdAndStatus(listingId, PeriodStatus.ACTIVE))
                .thenReturn(Optional.empty());
        when(ownershipShareRepository.findByCoownershipListing_IdAndOwnerIdIsNotNullOrderByIdAsc(listingId))
                .thenReturn(List.of(first, second));
        when(periodRepository.save(any(Period.class))).thenAnswer(invocation -> {
            Period period = invocation.getArgument(0);
            if (period.getId() == null) {
                period.setId(UUID.randomUUID());
            }
            return period;
        });

        service.triggerFilledOut(listing);

        ArgumentCaptor<Period> periodCaptor = ArgumentCaptor.forClass(Period.class);
        verify(periodRepository).save(periodCaptor.capture());
        Period savedPeriod = periodCaptor.getValue();
        assertThat(savedPeriod.getStatus()).isEqualTo(PeriodStatus.ACTIVE);
        assertThat(savedPeriod.getStartDate()).isEqualTo(LocalDate.of(2026, 4, 1));
        assertThat(savedPeriod.getEndDate()).isEqualTo(LocalDate.of(2026, 4, 30));
        assertThat(savedPeriod.getTotalIncome()).isEqualByComparingTo(BigDecimal.ZERO);

        @SuppressWarnings("unchecked")
        ArgumentCaptor<List<OwnershipSlot>> slotsCaptor = (ArgumentCaptor<List<OwnershipSlot>>) (ArgumentCaptor<?>)
                ArgumentCaptor.forClass(List.class);
        verify(ownershipSlotsRepository).saveAll(slotsCaptor.capture());
        List<OwnershipSlot> slots = slotsCaptor.getValue();
        assertThat(slots).hasSize(30);
        assertThat(slots.getFirst().getStatus()).isEqualTo(OwnershipSlotStatus.FOR_RENT);
        assertThat(slots.getFirst().getOwnerId()).isEqualTo(first.getOwnerId());
        assertThat(slots.get(1).getOwnerId()).isEqualTo(second.getOwnerId());

        verify(ownershipShareRepository).saveAll(eq(List.of(first, second)));
        verify(outboxEventService).save(eq("COOWNERSHIP_FILLED_OUT"), any());
    }

    @Test
    void applyBookingConfirmedSkipsMessageWhenNoActivePeriodFound() {
        UUID rentalListingId = UUID.randomUUID();
        when(periodRepository
                .findByRentalListingIdAndStatus(rentalListingId, PeriodStatus.ACTIVE))
                .thenReturn(Optional.empty());

        service.applyBookingConfirmed(
                rentalListingId,
                LocalDate.of(2026, 4, 10),
                LocalDate.of(2026, 4, 12),
                new BigDecimal("1000.00")
        );

        verify(ownershipSlotsRepository, never()).saveAll(any());
        verify(periodRepository, never()).save(any(Period.class));
    }

    @Test
    void settleFinishedPeriodsWithoutBookedSlotsSettlesPeriodAndCreatesNextOneWithoutSettlementEvent() {
        UUID listingId = UUID.randomUUID();
        UUID rentalListingId = UUID.randomUUID();

        CoownershipListing listing = new CoownershipListing();
        listing.setId(listingId);
        listing.setOwnerId(UUID.randomUUID());

        Period activePeriod = new Period();
        activePeriod.setId(UUID.randomUUID());
        activePeriod.setCoownershipListing(listing);
        activePeriod.setRentalListingId(rentalListingId);
        activePeriod.setStartDate(LocalDate.of(2026, 4, 1));
        activePeriod.setEndDate(LocalDate.of(2026, 4, 30));
        activePeriod.setStatus(PeriodStatus.ACTIVE);
        activePeriod.setTotalIncome(BigDecimal.ZERO);

        OwnershipShare share = new OwnershipShare();
        share.setId(UUID.randomUUID());
        share.setOwnerId(UUID.randomUUID());
        share.setTemplateDaysMask(0);

        when(periodRepository
                .findByStatusAndEndDateBefore(PeriodStatus.ACTIVE, LocalDate.of(2026, 4, 19)))
                .thenReturn(List.of(activePeriod));
        when(ownershipSlotsRepository.countBookedSlotsByOwner(activePeriod.getId())).thenReturn(List.of());
        when(periodRepository.save(any(Period.class)))
                .thenAnswer(invocation -> invocation.getArgument(0));
        when(ownershipShareRepository.findByCoownershipListing_IdAndOwnerIdIsNotNullOrderByIdAsc(listingId))
                .thenReturn(List.of(share));

        service.settleFinishedPeriods();

        assertThat(activePeriod.getStatus()).isEqualTo(PeriodStatus.SETTLED);

        ArgumentCaptor<Period> periodCaptor = ArgumentCaptor.forClass(Period.class);
        verify(periodRepository, org.mockito.Mockito.times(2)).save(periodCaptor.capture());
        List<Period> savedPeriods = periodCaptor.getAllValues();
        Period nextPeriod = savedPeriods.get(1);
        assertThat(nextPeriod.getStatus()).isEqualTo(PeriodStatus.ACTIVE);
        assertThat(nextPeriod.getStartDate()).isEqualTo(LocalDate.of(2026, 5, 1));
        assertThat(nextPeriod.getEndDate()).isEqualTo(LocalDate.of(2026, 5, 31));
        assertThat(nextPeriod.getRentalListingId()).isEqualTo(rentalListingId);

        @SuppressWarnings("unchecked")
        ArgumentCaptor<List<OwnershipSlot>> nextSlotsCaptor = (ArgumentCaptor<List<OwnershipSlot>>) (ArgumentCaptor<?>)
                ArgumentCaptor.forClass(List.class);
        verify(ownershipSlotsRepository).saveAll(nextSlotsCaptor.capture());
        assertThat(nextSlotsCaptor.getValue()).hasSize(31);

        verify(outboxEventService, never()).save(eq("PERIOD_SETTLEMENT_READY"), any());
    }
}
