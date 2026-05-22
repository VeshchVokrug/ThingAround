package ru.veshvokrug.coownership.input.validation;

import jakarta.validation.Constraint;
import jakarta.validation.Payload;

import java.lang.annotation.Documented;
import java.lang.annotation.Retention;
import java.lang.annotation.Target;

import static java.lang.annotation.ElementType.FIELD;
import static java.lang.annotation.ElementType.PARAMETER;
import static java.lang.annotation.RetentionPolicy.RUNTIME;

/**
 * Проверяет, что fundingDeadline находится в окне от 30 дней до 1 года от сегодняшней даты.
 *
 * @author Dmitrii Marchenko 19.04.2026
 */
@Documented
@Constraint(validatedBy = FundingDeadlineWindowValidator.class)
@Target({FIELD, PARAMETER})
@Retention(RUNTIME)
public @interface FundingDeadlineWindow {
    String message() default "Дата окончания сбора должна быть от 30 дней до 1 года от текущей даты";

    Class<?>[] groups() default {};

    Class<? extends Payload>[] payload() default {};
}
