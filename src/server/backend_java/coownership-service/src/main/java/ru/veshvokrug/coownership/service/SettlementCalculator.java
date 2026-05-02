package ru.veshvokrug.coownership.service;

import org.springframework.stereotype.Component;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.UUID;

/**
 * Калькулятор распределения дохода периода по количеству BOOKED-слотов.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Component
public class SettlementCalculator {
	public List<SettlementLine> calculate(BigDecimal totalIncome, Map<UUID, Long> bookedSlotsByOwner) {
		if (bookedSlotsByOwner == null ||
				bookedSlotsByOwner.isEmpty() ||
				totalIncome == null ||
				totalIncome.signum() <= 0) {
			return List.of();
		}

		if (bookedSlotsByOwner.values().stream().anyMatch(value -> value == null || value < 0)) {
			return List.of();
		}

		long totalBookedSlots = bookedSlotsByOwner.values().stream().mapToLong(Long::longValue).sum();
		if (totalBookedSlots <= 0) {
			return List.of();
		}

		List<SettlementLine> lines = new ArrayList<>(bookedSlotsByOwner.size());
		for (Map.Entry<UUID, Long> entry : bookedSlotsByOwner.entrySet()) {
			BigDecimal ownerBooked = BigDecimal.valueOf(entry.getValue());
			BigDecimal amount = totalIncome
					.multiply(ownerBooked)
					.divide(BigDecimal.valueOf(totalBookedSlots), 2, RoundingMode.HALF_UP);
			lines.add(new SettlementLine(entry.getKey(), entry.getValue(), amount));
		}
		return lines;
	}

	public record SettlementLine(UUID ownerId, long bookedSlots, BigDecimal amount) {
	}
}
