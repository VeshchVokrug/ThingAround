package ru.veshvokrug.coownership.service;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
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
import java.math.RoundingMode;
import java.time.Clock;
import java.time.LocalDate;
import java.time.YearMonth;
import java.time.temporal.ChronoUnit;
import java.util.*;

/**
 * Сервис жизненного цикла периода и связанных слотов владения.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Service
public class PeriodLifecycleService {
	private final PeriodRepository periodRepository;
	private final OwnershipSlotsRepository ownershipSlotsRepository;
	private final OwnershipShareRepository ownershipShareRepository;
	private final OutboxEventService outboxEventService;
	private final SettlementCalculator settlementCalculator;
	private final Clock clock;

	public PeriodLifecycleService(PeriodRepository periodRepository,
								  OwnershipSlotsRepository ownershipSlotsRepository,
								  OwnershipShareRepository ownershipShareRepository,
								  OutboxEventService outboxEventService,
								  SettlementCalculator settlementCalculator,
								  Clock clock) {
		this.periodRepository = periodRepository;
		this.ownershipSlotsRepository = ownershipSlotsRepository;
		this.ownershipShareRepository = ownershipShareRepository;
		this.outboxEventService = outboxEventService;
		this.settlementCalculator = settlementCalculator;
		this.clock = clock;
	}

	@Transactional
	public void triggerFilledOut(CoownershipListing listing) {
		if (periodRepository
				.findByCoownershipListing_IdAndStatus(
						listing.getId(),
						PeriodStatus.ACTIVE)
				.isPresent()) {
			return;
		}

		List<OwnershipShare> ownedShares = ownershipShareRepository
				.findByCoownershipListing_IdAndOwnerIdIsNotNullOrderByIdAsc(listing.getId());
		if (ownedShares.isEmpty()) {
			throw ServiceException.conflict("Нельзя запустить период без распределенных долей");
		}

		YearMonth currentMonth = YearMonth.from(LocalDate.now(clock));
		Period period = new Period();
		period.setCoownershipListing(listing);
		period.setStartDate(currentMonth.atDay(1));
		period.setEndDate(currentMonth.atEndOfMonth());
		period.setStatus(PeriodStatus.ACTIVE);
		period.setTotalIncome(BigDecimal.ZERO);
		periodRepository.save(period);

		createSlotsFromRoundRobinShares(period, ownedShares);

		outboxEventService.save("COOWNERSHIP_FILLED_OUT", new CoownershipFilledOutPayload(
				listing.getId(),
				listing.getCatalogListingId(),
				listing.getOwnerId(),
				period.getId(),
				period.getStartDate(),
				period.getEndDate()
		));
	}

	@Transactional
	public void linkRentalListing(UUID coownershipListingId, UUID rentalListingId) {
		periodRepository.findByCoownershipListing_IdAndStatus(coownershipListingId, PeriodStatus.ACTIVE)
				.ifPresent(period -> {
					period.setRentalListingId(rentalListingId);
					periodRepository.save(period);
				});
	}

	@Transactional
	public void applyBookingConfirmed(
			UUID rentalListingId,
			LocalDate startDate,
			LocalDate endDate,
			BigDecimal totalPrice) {
		periodRepository.findByRentalListingIdAndStatus(rentalListingId, PeriodStatus.ACTIVE)
				.ifPresent(period -> {
					List<OwnershipSlot> slots = ownershipSlotsRepository
							.findByPeriod_IdAndDateBetweenOrderByDateAsc(period.getId(), startDate, endDate);
					if (slots.isEmpty()) {
						return;
					}

					long bookingDays = ChronoUnit.DAYS.between(startDate, endDate) + 1;
					if (bookingDays <= 0) {
						return;
					}

					for (OwnershipSlot slot : slots) {
						slot.setStatus(OwnershipSlotStatus.BOOKED);
					}
					ownershipSlotsRepository.saveAll(slots);

					BigDecimal safePrice = totalPrice == null ? BigDecimal.ZERO : totalPrice;
					BigDecimal normalizedPrice = safePrice.setScale(2, RoundingMode.HALF_UP);

					BigDecimal bookedPrice = normalizedPrice
							.multiply(BigDecimal.valueOf(slots.size()))
							.divide(BigDecimal.valueOf(bookingDays), 2, RoundingMode.HALF_UP);
					period.setTotalIncome(period.getTotalIncome().add(bookedPrice));
					periodRepository.save(period);
				});
	}

	@Transactional
	public void settleFinishedPeriods() {
		LocalDate today = LocalDate.now(clock);
		List<Period> periodsToSettle = periodRepository
				.findByStatusAndEndDateBefore(PeriodStatus.ACTIVE, today);

		for (Period period : periodsToSettle) {
			settleSinglePeriod(period);
		}
	}

	private void settleSinglePeriod(Period period) {
		Map<UUID, Long> bookedSlotsByOwner = new LinkedHashMap<>();
		for (OwnershipSlotsRepository.BookedSlotsByOwnerProjection row : ownershipSlotsRepository
				.countBookedSlotsByOwner(period.getId())) {
			bookedSlotsByOwner.put(row.getOwnerId(), row.getSlotsCount());
		}

		List<SettlementCalculator.SettlementLine> lines = settlementCalculator
				.calculate(period.getTotalIncome(), bookedSlotsByOwner);
		if (!lines.isEmpty()) {
			outboxEventService.save("PERIOD_SETTLEMENT_READY", new PeriodSettlementReadyPayload(
					period.getId(),
					period.getCoownershipListing().getId(),
					period.getRentalListingId(),
					period.getStartDate(),
					period.getEndDate(),
					period.getTotalIncome(),
					lines
			));
		}

		period.setStatus(PeriodStatus.SETTLED);
		periodRepository.save(period);
		createNextPeriodFromTemplate(period);
	}

	private void createNextPeriodFromTemplate(Period settledPeriod) {
		YearMonth nextMonth = YearMonth.from(settledPeriod.getStartDate()).plusMonths(1);

		Period nextPeriod = new Period();
		nextPeriod.setCoownershipListing(settledPeriod.getCoownershipListing());
		nextPeriod.setRentalListingId(settledPeriod.getRentalListingId());
		nextPeriod.setStartDate(nextMonth.atDay(1));
		nextPeriod.setEndDate(nextMonth.atEndOfMonth());
		nextPeriod.setStatus(PeriodStatus.ACTIVE);
		nextPeriod.setTotalIncome(BigDecimal.ZERO);
		periodRepository.save(nextPeriod);

		List<OwnershipShare> shares = ownershipShareRepository
				.findByCoownershipListing_IdAndOwnerIdIsNotNullOrderByIdAsc(
						settledPeriod
								.getCoownershipListing()
								.getId());
		if (shares.isEmpty()) {
			return;
		}

		List<OwnershipSlot> slots = new ArrayList<>();
		int fallbackIndex = 0;
		for (LocalDate date = nextPeriod.getStartDate();
			 !date.isAfter(nextPeriod.getEndDate());
			 date = date.plusDays(1)) {
			OwnershipShare matched = findShareByTemplateMask(shares, date.getDayOfMonth());
			if (matched == null) {
				matched = shares.get(fallbackIndex % shares.size());
				fallbackIndex++;
			}

			createOwnershipSlot(nextPeriod, slots, date, matched);
		}
		ownershipSlotsRepository.saveAll(slots);
	}

	private void createOwnershipSlot(Period nextPeriod,
									 List<OwnershipSlot> slots,
									 LocalDate date,
									 OwnershipShare matched) {
		OwnershipSlot slot = new OwnershipSlot();
		slot.setPeriod(nextPeriod);
		slot.setOwnerId(matched.getOwnerId());
		slot.setDate(date);
		slot.setStatus(OwnershipSlotStatus.FOR_RENT);
		slot.setOverride(false);
		slots.add(slot);
	}

	private OwnershipShare findShareByTemplateMask(
			List<OwnershipShare> shares,
			int dayOfMonth) {
		int bit = 1 << (dayOfMonth - 1);
		for (OwnershipShare share : shares) {
			if ((share.getTemplateDaysMask() & bit) != 0) {
				return share;
			}
		}
		return null;
	}

	private void createSlotsFromRoundRobinShares(Period period, List<OwnershipShare> ownedShares) {
		List<OwnershipSlot> slots = new ArrayList<>();
		Map<UUID, Integer> templateMaskByShareId = new HashMap<>();

		int index = 0;
		for (LocalDate date = period.getStartDate();
			 !date.isAfter(period.getEndDate());
			 date = date.plusDays(1)) {
			OwnershipShare share = ownedShares.get(index % ownedShares.size());
			index++;

			createOwnershipSlot(period, slots, date, share);

			int dayBit = 1 << (date.getDayOfMonth() - 1);
			int currentMask = templateMaskByShareId.getOrDefault(share.getId(), 0);
			templateMaskByShareId.put(share.getId(), currentMask | dayBit);
		}

		for (OwnershipShare share : ownedShares) {
			share.setTemplateDaysMask(templateMaskByShareId.getOrDefault(share.getId(), 0));
		}

		ownershipSlotsRepository.saveAll(slots);
		ownershipShareRepository.saveAll(ownedShares);
	}

	private record CoownershipFilledOutPayload(UUID coownershipListingId,
											   UUID catalogListingId,
											   UUID ownerId,
											   UUID periodId,
											   LocalDate periodStartDate,
											   LocalDate periodEndDate) {
	}

	private record PeriodSettlementReadyPayload(UUID periodId,
												UUID coownershipListingId,
												UUID rentalListingId,
												LocalDate startDate,
												LocalDate endDate,
												BigDecimal totalIncome,
												Collection<SettlementCalculator.SettlementLine> settlements) {
	}
}
