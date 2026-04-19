package ru.veshvokrug.coownership.input.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;

import java.math.BigDecimal;
import java.util.List;
import java.util.UUID;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
public record CoownershipListingDto(
        @NotNull
        UUID catalogListingId,

        @NotNull
        BigDecimal price,

        @NotNull
        UUID ownerId,
        @Min(2)
        @Max(10)
        int totalShares,

        List<ShareApplicationDto> shareSlots
) {
}
