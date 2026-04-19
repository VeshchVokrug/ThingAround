package ru.veshvokrug.coownership.input.dto;

import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;

import java.util.UUID;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
public record ShareApplicationDto(
        @NotNull
        UUID applicantId,
        @NotNull
        @Min(value = 1, message = "Количество долей должно быть больше 0")
        int sharesCount,
        @NotNull
        ShareApplicationStatus status
) {
}
