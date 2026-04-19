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
        @NotNull
        @Size(
                min = 8,
                max = 128,
                message = "Название должно быть в диапазоне от 8 до 128 символов")
        @Pattern(regexp = "^[^<>]*$", message = "Название не должно содержать HTML-теги")
        @Schema(
                description = "Название листинга совладения",
                example = "Апартаменты у моря",
                requiredMode = Schema.RequiredMode.REQUIRED,
                minLength = 8,
                maxLength = 128)
        String name,

        @Size(
                max = 1024,
                message = "Описание должно быть не больше 1024 символов")
        @Pattern(regexp = "^[^<>]*$", message = "Описание не должно содержать HTML-теги")
        @Schema(
                description = "Описание объекта",
                example = "Квартира в центре города с видом на парк",
                maxLength = 1024)
        String description,

        @NotNull
        @DecimalMin(value = "0.00", message = "Цена доли не может быть отрицательной")
        @Schema(description = "Цена одной доли", example = "150000.00", requiredMode = Schema.RequiredMode.REQUIRED, minimum = "0.00")
        BigDecimal sharePrice,

        @NotNull
        @Schema(description = "ID создателя листинга", example = "8b8a6c2d-1d8f-4c5e-a8d7-4d2fbf4a9c11", requiredMode = Schema.RequiredMode.REQUIRED)
        UUID ownerId,

        @Min(2)
        @Max(10)
        @Schema(description = "Общее количество долей", example = "10", requiredMode = Schema.RequiredMode.REQUIRED, minimum = "2", maximum = "10")
        int totalShares,

        @FundingDeadlineWindow
        @Schema(description = "Дата завершения сбора. Допустимое окно: от +30 дней до +1 года. Если не указана, сервис подставляет текущая дата + 90 дней.", example = "2026-06-20", type = "string", format = "date")
        LocalDate fundingDeadline
) {
}
