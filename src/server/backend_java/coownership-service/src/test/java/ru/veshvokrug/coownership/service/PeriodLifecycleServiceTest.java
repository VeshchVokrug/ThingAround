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
import ru.veshvokrug.coownership.output.repository.CoownershipListingRepository;
import ru.veshvokrug.coownership.output.repository.OwnershipShareRepository;
import ru.veshvokrug.coownership.output.repository.OwnershipSlotsRepository;
import ru.veshvokrug.coownership.output.repository.PeriodRepository;
import ru.veshvokrug.coownership.service.outbox.OutboxEventService;

import java.lang.reflect.Method;
import java.math.BigDecimal;
import java.time.Clock;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.Collection;
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
    private CoownershipListingRepository coownershipListingRepository;

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
                Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC),
                coownershipListingRepository
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
    void linkRentalListingLinksActivePeriod() {
        UUID coownershipListingId = UUID.randomUUID();
        UUID rentalListingId = UUID.randomUUID();
        Period period = new Period();
        period.setId(UUID.randomUUID());
        when(periodRepository
                .findByCoownershipListing_IdAndStatus(coownershipListingId, PeriodStatus.ACTIVE))
                .thenReturn(Optional.of(period));

        service.linkRentalListing(coownershipListingId, rentalListingId);

        assertThat(period.getRentalListingId()).isEqualTo(rentalListingId);
        verify(periodRepository).save(period);
    }

    @Test
    void linkRentalListingThrowsWhenNoActivePeriodSoEventIsRetried() {
        // Тихий пропуск помечал бы событие обработанным и связь терялась бы
        // навсегда — исключение откатывает идемпотентную транзакцию
        UUID coownershipListingId = UUID.randomUUID();
        when(periodRepository
                .findByCoownershipListing_IdAndStatus(coownershipListingId, PeriodStatus.ACTIVE))
                .thenReturn(Optional.empty());

        org.assertj.core.api.Assertions.assertThatThrownBy(
                        () -> service.linkRentalListing(coownershipListingId, UUID.randomUUID()))
                .isInstanceOf(ServiceException.class);
        verify(periodRepository, never()).save(any(Period.class));
    }

    @Test
    void applyBookingConfirmedSkipsPersonalUseAndAlreadyBookedSlots() {
        UUID rentalListingId = UUID.randomUUID();
        Period period = new Period();
        period.setId(UUID.randomUUID());
        period.setTotalIncome(BigDecimal.ZERO);
        when(periodRepository
                .findByRentalListingIdAndStatus(rentalListingId, PeriodStatus.ACTIVE))
                .thenReturn(Optional.of(period));

        OwnershipSlot forRent = new OwnershipSlot();
        forRent.setStatus(OwnershipSlotStatus.FOR_RENT);
        OwnershipSlot personalUse = new OwnershipSlot();
        personalUse.setStatus(OwnershipSlotStatus.PERSONAL_USE);
        OwnershipSlot alreadyBooked = new OwnershipSlot();
        alreadyBooked.setStatus(OwnershipSlotStatus.BOOKED);
        when(ownershipSlotsRepository.findByPeriod_IdAndDateBetweenOrderByDateAsc(
                eq(period.getId()), any(LocalDate.class), any(LocalDate.class)))
                .thenReturn(List.of(forRent, personalUse, alreadyBooked));

        service.applyBookingConfirmed(
                rentalListingId,
                LocalDate.of(2026, 4, 10),
                LocalDate.of(2026, 4, 12),
                new BigDecimal("3000.00")
        );

        // Из 3 дней бронирования монетизирован только FOR_RENT-слот: 3000 * 1/3
        assertThat(forRent.getStatus()).isEqualTo(OwnershipSlotStatus.BOOKED);
        assertThat(personalUse.getStatus()).isEqualTo(OwnershipSlotStatus.PERSONAL_USE);
        assertThat(period.getTotalIncome()).isEqualByComparingTo(new BigDecimal("1000.00"));
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
    void applyBookingConfirmedCountsOnlySlotsInsideCurrentPeriod() {
        UUID rentalListingId = UUID.randomUUID();

        CoownershipListing listing = new CoownershipListing();
        listing.setId(UUID.randomUUID());
        listing.setOwnerId(UUID.randomUUID());

        Period activePeriod = new Period();
        activePeriod.setId(UUID.randomUUID());
        activePeriod.setCoownershipListing(listing);
        activePeriod.setRentalListingId(rentalListingId);
        activePeriod.setStartDate(LocalDate.of(2026, 4, 1));
        activePeriod.setEndDate(LocalDate.of(2026, 4, 30));
        activePeriod.setStatus(PeriodStatus.ACTIVE);
        activePeriod.setTotalIncome(new BigDecimal("200.00"));

        OwnershipSlot first = new OwnershipSlot();
        first.setId(UUID.randomUUID());
        first.setStatus(OwnershipSlotStatus.FOR_RENT);
        OwnershipSlot second = new OwnershipSlot();
        second.setId(UUID.randomUUID());
        second.setStatus(OwnershipSlotStatus.FOR_RENT);
        OwnershipSlot third = new OwnershipSlot();
        third.setId(UUID.randomUUID());
        third.setStatus(OwnershipSlotStatus.FOR_RENT);

        when(periodRepository.findByRentalListingIdAndStatus(rentalListingId, PeriodStatus.ACTIVE))
                .thenReturn(Optional.of(activePeriod));
        when(ownershipSlotsRepository.findByPeriod_IdAndDateBetweenOrderByDateAsc(
                activePeriod.getId(),
                LocalDate.of(2026, 3, 30),
                LocalDate.of(2026, 4, 3)))
                .thenReturn(List.of(first, second, third));
        when(periodRepository.save(any(Period.class)))
                .thenAnswer(invocation -> invocation.getArgument(0));

        service.applyBookingConfirmed(
                rentalListingId,
                LocalDate.of(2026, 3, 30),
                LocalDate.of(2026, 4, 3),
                new BigDecimal("100.00")
        );

        assertThat(activePeriod.getTotalIncome()).isEqualByComparingTo("260.00");
        verify(ownershipSlotsRepository).saveAll(List.of(first, second, third));
        verify(periodRepository).save(activePeriod);
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

    @Test
    void settleFinishedPeriodsWithBookedSlotsSendsSettlementReadyAndCreatesNextPeriod() throws Exception {
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
        activePeriod.setTotalIncome(new BigDecimal("1000.00"));

        OwnershipShare share = new OwnershipShare();
        share.setId(UUID.randomUUID());
        UUID ownerId = UUID.randomUUID();
        share.setOwnerId(ownerId);
        share.setTemplateDaysMask(0);

        when(periodRepository
                .findByStatusAndEndDateBefore(PeriodStatus.ACTIVE, LocalDate.of(2026, 4, 19)))
                .thenReturn(List.of(activePeriod));

        // prepare projection instance for booked slots count
        OwnershipSlotsRepository.BookedSlotsByOwnerProjection proj =
                new OwnershipSlotsRepository.BookedSlotsByOwnerProjection() {
            @Override
            public UUID getOwnerId() {
                return ownerId;
            }

            @Override
            public long getSlotsCount() {
                return 10L;
            }
        };

        when(ownershipSlotsRepository.countBookedSlotsByOwner(activePeriod.getId()))
                .thenReturn(List.of(proj));
        when(periodRepository.save(any(Period.class)))
                .thenAnswer(invocation -> invocation.getArgument(0));
        when(ownershipShareRepository.findByCoownershipListing_IdAndOwnerIdIsNotNullOrderByIdAsc(listingId))
                .thenReturn(List.of(share));

        service.settleFinishedPeriods();

        // period must be settled
        assertThat(activePeriod.getStatus()).isEqualTo(PeriodStatus.SETTLED);

        // outbox event must be sent and payload must contain expected fields
        ArgumentCaptor<Object> payloadCaptor = ArgumentCaptor.forClass(Object.class);
        verify(outboxEventService).save(eq("PERIOD_SETTLEMENT_READY"), payloadCaptor.capture());
        Object payload = payloadCaptor.getValue();

        // payload is a private record inside PeriodLifecycleService; use reflection to inspect
        Class<?> payloadClass = payload.getClass();
        Method periodIdMethod = payloadClass.getMethod("periodId");
        UUID capturedPeriodId = (UUID) periodIdMethod.invoke(payload);
        assertThat(capturedPeriodId).isEqualTo(activePeriod.getId());

        Method totalIncomeMethod = payloadClass.getMethod("totalIncome");
        BigDecimal capturedTotalIncome = (BigDecimal) totalIncomeMethod.invoke(payload);
        assertThat(capturedTotalIncome).isEqualByComparingTo(new BigDecimal("1000.00"));

        Method settlementsMethod = payloadClass.getMethod("settlements");
        @SuppressWarnings("unchecked")
        java.util.Collection<Object> settlements = (Collection<Object>) settlementsMethod.invoke(payload);
        assertThat(settlements).hasSize(1);

        Object firstLine = settlements.iterator().next();
        Class<?> lineClass = firstLine.getClass();
        Method ownerIdMethod = lineClass.getMethod("ownerId");
        Method bookedSlotsMethod = lineClass.getMethod("bookedSlots");
        Method amountMethod = lineClass.getMethod("amount");

        java.util.UUID capturedOwnerId = (UUID) ownerIdMethod.invoke(firstLine);
        long capturedBookedSlots = (long) bookedSlotsMethod.invoke(firstLine);
        BigDecimal capturedAmount = (BigDecimal) amountMethod.invoke(firstLine);

        assertThat(capturedOwnerId).isEqualTo(ownerId);
        assertThat(capturedBookedSlots).isEqualTo(10L);
        assertThat(capturedAmount).isEqualByComparingTo(new BigDecimal("1000.00"));

        // next period must be created
        ArgumentCaptor<Period> periodCaptor = ArgumentCaptor.forClass(Period.class);
        verify(periodRepository, atLeast(2)).save(periodCaptor.capture());
        List<Period> savedPeriods = periodCaptor.getAllValues();
        Period nextPeriod = savedPeriods.getLast();
        assertThat(nextPeriod.getStatus()).isEqualTo(PeriodStatus.ACTIVE);
        assertThat(nextPeriod.getStartDate()).isEqualTo(LocalDate.of(2026, 5, 1));
        assertThat(nextPeriod.getEndDate()).isEqualTo(LocalDate.of(2026, 5, 31));
        assertThat(nextPeriod.getRentalListingId()).isEqualTo(rentalListingId);
    }
}
