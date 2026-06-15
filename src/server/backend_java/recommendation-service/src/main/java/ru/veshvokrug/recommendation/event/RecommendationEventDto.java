package ru.veshvokrug.recommendation.event;

import com.fasterxml.jackson.annotation.JsonProperty;

/**
 * DTO события рекомендаций, которое потребляется из RabbitMQ.
 * Описывает пользовательскую активность: просмотры, избранное, бронирования и т.д.
 *
 * @author Dmitrii Marchenko
 */
public record RecommendationEventDto(
        @JsonProperty("eventId")
        String eventId,

        @JsonProperty("userId")
        String userId,

        @JsonProperty("eventType")
        String eventType,

        @JsonProperty("categorySlug")
        String categorySlug,

        @JsonProperty("listingId")
        String listingId,

        @JsonProperty("timestamp")
        Long timestamp
) {
    /** Проверяет, что событие содержит обязательные поля. */
    public boolean isValid() {
        return eventId != null && !eventId.isBlank() &&
                userId != null && !userId.isBlank() &&
                eventType != null && !eventType.isBlank() &&
                timestamp != null && timestamp > 0;
    }

    /** Проверяет, есть ли у события категория. */
    public boolean hasCategorySlug() {
        return categorySlug != null && !categorySlug.isBlank();
    }

    /** Проверяет, привязано ли событие к конкретному объявлению. */
    public boolean hasListing() {
        return listingId != null && !listingId.isBlank();
    }
}

