package ru.veshvokrug.recommendation.controller.dto;

import java.math.BigDecimal;
import java.util.List;

/**
 * @author Dmitrii Marchenko 06.04.2026
 */
public record Listing(String listingId,
                      String title,
                      String categorySlug,
                      BigDecimal basePricePerDay,
                      List<String> imageUrls,
                      double ownerRating,
                      double score) {
}
