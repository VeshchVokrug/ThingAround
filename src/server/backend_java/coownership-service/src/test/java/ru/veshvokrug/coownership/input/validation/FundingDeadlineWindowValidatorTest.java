package ru.veshvokrug.coownership.input.validation;

import org.junit.jupiter.api.Test;

import java.time.LocalDate;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Тесты валидатора окна дат fundingDeadline.
 * <p>
 * Проверяются граничные и выходящие за диапазон значения.
 */
class FundingDeadlineWindowValidatorTest {

    private final FundingDeadlineWindowValidator validator = new FundingDeadlineWindowValidator();

    @Test
    void acceptsNullValue() {
        assertThat(validator.isValid(null, null)).isTrue();
    }

    @Test
    void acceptsLowerBoundaryPlusThirtyDays() {
        LocalDate value = LocalDate.now().plusDays(30);
        assertThat(validator.isValid(value, null)).isTrue();
    }

    @Test
    void acceptsUpperBoundaryPlusOneYear() {
        LocalDate value = LocalDate.now().plusYears(1);
        assertThat(validator.isValid(value, null)).isTrue();
    }

    @Test
    void rejectsBeforeLowerBoundary() {
        LocalDate value = LocalDate.now().plusDays(29);
        assertThat(validator.isValid(value, null)).isFalse();
    }

    @Test
    void rejectsAfterUpperBoundary() {
        LocalDate value = LocalDate.now().plusYears(1).plusDays(1);
        assertThat(validator.isValid(value, null)).isFalse();
    }
}
