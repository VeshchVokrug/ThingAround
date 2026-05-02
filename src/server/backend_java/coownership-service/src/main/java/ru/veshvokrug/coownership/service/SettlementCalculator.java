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

		BigDecimal normalizedIncome = totalIncome.setScale(2, RoundingMode.HALF_UP);
		List<Map.Entry<UUID, Long>> owners = new ArrayList<>(bookedSlotsByOwner.entrySet());
		List<SettlementLine> lines = new ArrayList<>(owners.size());
		BigDecimal allocated = BigDecimal.ZERO;
		for (int i = 0; i < owners.size(); i++) {
			Map.Entry<UUID, Long> entry = owners.get(i);
			BigDecimal amount;
			if (i == owners.size() - 1) {
				amount = normalizedIncome.subtract(allocated).setScale(2, RoundingMode.HALF_UP);
			} else {
				BigDecimal ownerBooked = BigDecimal.valueOf(entry.getValue());
				amount = normalizedIncome
						.multiply(ownerBooked)
						.divide(BigDecimal.valueOf(totalBookedSlots), 2, RoundingMode.HALF_UP);
				allocated = allocated.add(amount);
			}
			lines.add(new SettlementLine(entry.getKey(), entry.getValue(), amount));
		}
		return lines;
	}

	public record SettlementLine(UUID ownerId, long bookedSlots, BigDecimal amount) {
	}
}
