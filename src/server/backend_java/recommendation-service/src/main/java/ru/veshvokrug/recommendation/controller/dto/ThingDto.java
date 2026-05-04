package ru.veshvokrug.recommendation.controller.dto;

import java.time.Instant;
import java.util.List;

/**
 * @author Dmitrii Marchenko 06.04.2026
 */
public record ThingDto(String userId,
                       Instant updatedAt,
                       boolean isColdStart,
                       List<Listing> listings) {
}
