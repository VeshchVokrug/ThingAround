package ru.veshvokrug.coownership.input.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import ru.veshvokrug.coownership.model.CoownershipStatus;

import java.time.LocalDate;
import java.util.UUID;

/**
 * Выходной DTO для create-операции листинга совладения.
 *
 * @author Dmitrii Marchenko 19.04.2026
 */
public record CoownershipListingCreateResponseDto(
        @Schema(description = "ID созданного листинга", example = "2f5f1a0f-6f1f-4ae2-b5e8-2f1b7f3b7d20")
        UUID id,
        @Schema(description = "Дата завершения сбора", example = "2026-07-18", type = "string", format = "date")
        LocalDate fundingDeadline,
        @Schema(description = "Текущий статус листинга", example = "OPEN")
        CoownershipStatus status
) {
}
