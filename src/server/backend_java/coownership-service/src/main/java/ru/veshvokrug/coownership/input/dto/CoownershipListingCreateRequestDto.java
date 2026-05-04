package ru.veshvokrug.coownership.input.dto;

import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;
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
        UUID catalogListingId,

        @NotNull
        @DecimalMin(value = "0.00", message = "Цена доли не может быть отрицательной")
        BigDecimal price,

        @NotNull
        UUID ownerId,

        @Min(2)
        @Max(10)
        int totalShares,

        @FundingDeadlineWindow
        LocalDate fundingDeadline
) {
}
