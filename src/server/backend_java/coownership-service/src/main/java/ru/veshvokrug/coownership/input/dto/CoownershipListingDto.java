package ru.veshvokrug.coownership.input.dto;

import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

import java.math.BigDecimal;
import java.util.List;
import java.util.UUID;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
public record CoownershipListingDto(
        @NotNull
        @Size(
                min = 8,
                max = 128,
                message = "Название должно быть в диапазоне от 8 до 128 символов")
        String name,

        @Size(
                max = 1024,
                message = "Описание должно быть не больше 1024 символов")
        String description,

        //todo: добавить фото

        @NotNull
        BigDecimal sharePrice,

        @NotNull
        UUID ownerId,
        @Min(2)
        @Max(10)
        int totalShares,

        List<ShareApplicationDto> shareSlots
) {
}
