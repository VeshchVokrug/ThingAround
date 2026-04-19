package ru.veshvokrug.coownership.input.validation;

import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;

import java.time.LocalDate;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
public class FundingDeadlineWindowValidator implements ConstraintValidator<FundingDeadlineWindow, LocalDate> {
    @Override
    public boolean isValid(LocalDate value, ConstraintValidatorContext context) {
        if (value == null) {
            return true;
        }

        LocalDate today = LocalDate.now();
        LocalDate min = today.plusDays(30);
        LocalDate max = today.plusYears(1);
        return !value.isBefore(min) && !value.isAfter(max);
    }
}
