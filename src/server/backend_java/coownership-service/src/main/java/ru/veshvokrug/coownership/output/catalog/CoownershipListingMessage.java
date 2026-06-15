package ru.veshvokrug.coownership.output.catalog;

import com.fasterxml.jackson.annotation.JsonFormat;
import com.fasterxml.jackson.annotation.JsonProperty;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;

import java.math.RoundingMode;
import java.time.Instant;
import java.time.LocalDate;
import java.util.List;
import java.util.UUID;

/**
 * Контракт сообщения для catalog-service.
 * Зеркало C# класса {@code Core.Events.CoownershipListingMessage}.
 * Имена JSON-полей зафиксированы аннотациями под camelCase-политику
 * System.Text.Json, которую использует MassTransit.
 *
 * @author Dmitrii Marchenko
 */
public record CoownershipListingMessage(
        @JsonProperty("action") CoownershipListingAction action,
        @JsonProperty("listingId") UUID listingId,
        @JsonProperty("ownerId") UUID ownerId,
        @JsonProperty("catalogListingId") UUID catalogListingId,
        @JsonProperty("categorySlug") String categorySlug,
        @JsonProperty("title") String title,
        @JsonProperty("description") String description,
        @JsonProperty("imagesUrls") List<String> imagesUrls,
        @JsonProperty("city") String city,
        @JsonProperty("sharePrice") int sharePrice,
        @JsonProperty("totalShares") int totalShares,
        @JsonProperty("availableShares") int availableShares,
        @JsonProperty("fundingDeadline")
        @JsonFormat(shape = JsonFormat.Shape.STRING, pattern = "yyyy-MM-dd")
        LocalDate fundingDeadline,
        @JsonProperty("isActive") boolean isActive,
        @JsonProperty("version") int version,
        @JsonProperty("titleSlug") String titleSlug,
        @JsonProperty("createdAt")
        @JsonFormat(shape = JsonFormat.Shape.STRING, timezone = "UTC")
        Instant createdAt,
        @JsonProperty("updatedAt")
        @JsonFormat(shape = JsonFormat.Shape.STRING, timezone = "UTC")
        Instant updatedAt
) {

    /**
     * Собирает сообщение из текущего состояния листинга.
     * titleSlug всегда пустой: catalog-service генерирует slug сам
     * и на Create, и на Update.
     */
    public static CoownershipListingMessage from(CoownershipListingAction action, CoownershipListing listing) {
        return new CoownershipListingMessage(
                action,
                listing.getId(),
                listing.getOwnerId(),
                listing.getCatalogListingId(),
                listing.getCategorySlug(),
                listing.getTitle(),
                listing.getDescription(),
                listing.getImagesUrls(),
                listing.getCity(),
                toSharePrice(listing),
                listing.getTotalShares(),
                Math.max(0, listing.getTotalShares() - listing.getFilledShares()),
                listing.getFundingDeadline(),
                listing.getStatus() != CoownershipStatus.CANCELLED,
                (int) listing.getVersion(),
                "",
                listing.getCreatedAt(),
                listing.getUpdatedAt()
        );
    }

    /**
     * Контракт каталога хранит цену доли как int —
     * округляем BigDecimal до целых рублей.
     */
    private static int toSharePrice(CoownershipListing listing) {
        return listing.getPrice() == null
                ? 0
                : listing.getPrice().setScale(0, RoundingMode.HALF_UP).intValue();
    }
}
