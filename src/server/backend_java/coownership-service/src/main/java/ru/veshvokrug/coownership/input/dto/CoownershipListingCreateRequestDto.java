package ru.veshvokrug.coownership.input.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.*;
import ru.veshvokrug.coownership.input.validation.FundingDeadlineWindow;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.UUID;

/**
 * Входной DTO для создания листинга совладения.
 *
 * @author Dmitrii Marchenko 19.04.2026
 */
public record CoownershipListingCreateRequestDto(
        @NotBlank
        @Size(max = 128)
        @Schema(
                description = "Название объекта для листинга",
                example = "Дом у моря",
                requiredMode = Schema.RequiredMode.REQUIRED,
                maxLength = 128)
        String name,

        @Size(max = 1024)
        @Schema(
                description = "Описание объекта. Необязательное поле до 1024 символов",
                example = "Коттедж с участком и видом на море",
                requiredMode = Schema.RequiredMode.NOT_REQUIRED,
                maxLength = 1024)
        String description,

        @NotNull
        @Schema(
                description = "ID листинга из Catalog",
                example = "2f5f1a0f-6f1f-4ae2-b5e8-2f1b7f3b7d20",
                requiredMode = Schema.RequiredMode.REQUIRED)
        UUID catalogListingId,

        @NotNull
        @DecimalMin(value = "0.00", message = "Цена доли не может быть отрицательной")
        @Schema(
                description = "Цена объекта для бизнес-расчетов",
                example = "150000.00",
                requiredMode = Schema.RequiredMode.REQUIRED,
                minimum = "0.00")
        BigDecimal price,

        @NotNull
        @Schema(
                description = "ID создателя листинга",
                example = "8b8a6c2d-1d8f-4c5e-a8d7-4d2fbf4a9c11",
                requiredMode = Schema.RequiredMode.REQUIRED)
        UUID ownerId,

        @Min(2)
        @Max(10)
        @Schema(
                description = "Общее количество долей",
                example = "10",
                requiredMode = Schema.RequiredMode.REQUIRED,
                minimum = "2",
                maximum = "10")
        int totalShares,

        @FundingDeadlineWindow
        @Schema(
                description = "Дата завершения сбора. Допустимое окно: " +
                        "от +30 дней до +1 года. Если не указана, сервис " +
                        "подставляет текущая дата + 90 дней.",
                example = "2026-06-20",
                type = "string",
                format = "date")
        LocalDate fundingDeadline
) {
}
