package ru.veshvokrug.coownership.input.dto;

import io.swagger.v3.oas.annotations.media.Schema;
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
        @Schema(
                description = "ID пользователя, который подает заявку",
                example = "0c2f8f92-4f46-4c4f-aea3-a7f62814f8b0",
                requiredMode = Schema.RequiredMode.REQUIRED
        )
        UUID applicantId,

        @Min(value = 1, message = "Количество долей должно быть больше 0")
        @Schema(
                description = "Количество долей для покупки",
                example = "2",
                requiredMode = Schema.RequiredMode.REQUIRED,
                minimum = "1"
        )
        int sharesCount
) {
}
