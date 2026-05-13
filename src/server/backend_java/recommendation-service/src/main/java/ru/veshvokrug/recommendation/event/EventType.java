package ru.veshvokrug.recommendation.event;

/**
 * Перечень типов событий с привязкой к весам.
 * Для каждого события задаются веса интереса пользователя и популярности объявления.
 *
 * @author Dmitrii Marchenko
 */
public enum EventType {
    USER_CATEGORIES_UPDATED("UserCategoriesUpdated", 3.0, 0.0),
    LISTING_VIEWED("ListingViewed", 0.1, 1.0),
    SEARCH_PERFORMED("SearchPerformed", 0.3, 0.0),
    LISTING_FAVORITED("ListingFavorited", 0.7, 5.0),
    BOOKING_CREATED("BookingCreated", 1.5, 10.0),
    BOOKING_CONFIRMED("BookingConfirmed", 2.0, 20.0),
    BOOKING_COMPLETED("BookingCompleted", 2.5, 25.0),
    BOOKING_CANCELLED("BookingCancelled", -1.0, -10.0);

    private final String value;
    private final double userInterestWeight;
    private final double listingPopularityWeight;

    EventType(String value, double userInterestWeight, double listingPopularityWeight) {
        this.value = value;
        this.userInterestWeight = userInterestWeight;
        this.listingPopularityWeight = listingPopularityWeight;
    }

    public String getValue() {
        return value;
    }

    public double getUserInterestWeight() {
        return userInterestWeight;
    }

    public double getListingPopularityWeight() {
        return listingPopularityWeight;
    }

    /**
     * Преобразует строковое значение в тип события.
     * @param value строковое значение
     * @return тип события или {@code null}, если совпадение не найдено
     */
    public static EventType fromValue(String value) {
        if (value == null || value.isBlank()) {
            return null;
        }
        for (EventType type : EventType.values()) {
            if (type.value.equals(value)) {
                return type;
            }
        }
        return null;
    }
}

