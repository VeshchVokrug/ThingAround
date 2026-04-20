package ru.veshvokrug.coownership.input.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;

import java.util.UUID;

/**
 * Ответ по заявке на доли.
 *
 * @author Dmitrii Marchenko 20.04.2026
 */
public record ShareApplicationResponseDto(
        @Schema(description = "ID заявки", example = "4e26ec2a-ea6e-44ab-ae30-fca64e9353d2")
        UUID id,
        @Schema(description = "ID листинга", example = "896f9b82-c2c4-4f2d-b4f4-4301029ab71c")
        UUID listingId,
        @Schema(description = "ID заявителя", example = "0c2f8f92-4f46-4c4f-aea3-a7f62814f8b0")
        UUID applicantId,
        @Schema(description = "Запрошенное количество долей", example = "3")
        int sharesCount,
        @Schema(description = "Статус заявки", example = "PENDING")
        ShareApplicationStatus status
) {
}
