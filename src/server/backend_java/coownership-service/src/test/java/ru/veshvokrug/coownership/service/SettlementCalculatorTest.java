package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;

class SettlementCalculatorTest {
    private final SettlementCalculator calculator = new SettlementCalculator();

    @Test
    void shouldReturnEmptyWhenNoBookedSlots() {
        List<SettlementCalculator.SettlementLine> result = calculator.calculate(
                new BigDecimal("1000.00"),
                Map.of()
        );

        assertThat(result).isEmpty();
    }

    @Test
    void shouldReturnEmptyWhenIncomeIsNullOrNonPositive() {
        Map<UUID, Long> bookedSlots = Map.of(UUID.randomUUID(), 2L);

        assertThat(calculator.calculate(null, bookedSlots)).isEmpty();
        assertThat(calculator.calculate(BigDecimal.ZERO, bookedSlots)).isEmpty();
        assertThat(calculator.calculate(new BigDecimal("-1.00"), bookedSlots)).isEmpty();
    }

    @Test
    void shouldReturnEmptyWhenBookedSlotsContainInvalidValues() {
        Map<UUID, Long> bookedSlotsWithNegativeValue = Map.of(UUID.randomUUID(), -1L);

        assertThat(calculator.calculate(new BigDecimal("1000.00"), null)).isEmpty();
        assertThat(calculator.calculate(new BigDecimal("1000.00"), bookedSlotsWithNegativeValue)).isEmpty();
    }

    @Test
    void shouldSplitIncomeByBookedSlots() {
        UUID owner1 = UUID.randomUUID();
        UUID owner2 = UUID.randomUUID();
        Map<UUID, Long> bookedSlots = new LinkedHashMap<>();
        bookedSlots.put(owner1, 2L);
        bookedSlots.put(owner2, 1L);

        List<SettlementCalculator.SettlementLine> result = calculator.calculate(
                new BigDecimal("300.00"),
                bookedSlots
        );

        assertThat(result).hasSize(2);
        assertThat(result.get(0).ownerId()).isEqualTo(owner1);
        assertThat(result.get(0).amount()).isEqualByComparingTo("200.00");
        assertThat(result.get(1).ownerId()).isEqualTo(owner2);
        assertThat(result.get(1).amount()).isEqualByComparingTo("100.00");
    }

    @Test
    void shouldPreserveTotalIncomeAfterRounding() {
        UUID owner1 = UUID.randomUUID();
        UUID owner2 = UUID.randomUUID();
        UUID owner3 = UUID.randomUUID();
        Map<UUID, Long> bookedSlots = new LinkedHashMap<>();
        bookedSlots.put(owner1, 1L);
        bookedSlots.put(owner2, 1L);
        bookedSlots.put(owner3, 1L);

        List<SettlementCalculator.SettlementLine> result = calculator.calculate(
                new BigDecimal("100.00"),
                bookedSlots
        );

        assertThat(result).hasSize(3);
        assertThat(result).extracting(SettlementCalculator.SettlementLine::amount)
                .containsExactly(
                        new BigDecimal("33.33"),
                        new BigDecimal("33.33"),
                        new BigDecimal("33.34")
                );
        assertThat(result.stream().map(SettlementCalculator.SettlementLine::amount)
                .reduce(BigDecimal.ZERO, BigDecimal::add))
                .isEqualByComparingTo("100.00");
    }
}
