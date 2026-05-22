package ru.veshvokrug.coownership.input.dto;

import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;

import java.util.UUID;

/**
 * Запрос на создание заявки на покупку долей.
 *
 * @author Dmitrii Marchenko 20.04.2026
 */
public record ShareApplicationCreateRequestDto(
        @NotNull
        UUID applicantId,

        @Min(value = 1, message = "Количество долей должно быть больше 0")
        int sharesCount
) {
}
