package ru.veshvokrug.coownership.output.dto;

import ru.veshvokrug.coownership.model.AnnouncementStatus;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;

/**
 * @author Dmitrii Marchenko 06.04.2026
 */
public record AnnouncementResponseDto(
        String listingId,
        String ownerId,
        String title,
        String description,
        String categorySlug,
        List<String> imageUrls,
        BigDecimal totalTargetAmount,
        BigDecimal sharePrice,
        int filledShares,
        int totalShares,
        AnnouncementStatus announcementStatus,
        LocalDate fundingDeadline,
        double ownerRating,
        List<ShareResponseDto> shareResponseDto) {
}
