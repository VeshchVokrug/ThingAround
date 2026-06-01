package ru.veshvokrug.coownership.input.http.dto;

import ru.veshvokrug.coownership.model.CoownershipStatus;

import java.math.BigDecimal;
import java.time.Instant;
import java.time.LocalDate;
import java.util.UUID;

/**
 * Публичная проекция листинга для просмотра потенциальными совладельцами.
 */
public record PublicListingResponseDto(
        UUID id,
        UUID catalogListingId,
        UUID ownerId,
        BigDecimal price,
        int totalShares,
        int filledShares,
        int availableShares,
        CoownershipStatus status,
        LocalDate fundingDeadline,
        Instant createdAt
) {
}

