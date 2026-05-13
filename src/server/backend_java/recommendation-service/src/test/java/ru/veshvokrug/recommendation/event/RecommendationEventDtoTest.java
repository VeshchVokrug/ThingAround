package ru.veshvokrug.recommendation.event;

import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.*;

/**
 * Тесты для {@link RecommendationEventDto}.
 */
class RecommendationEventDtoTest {

    @Test
    void shouldValidateCorrectEvent() {
        RecommendationEventDto event = new RecommendationEventDto("e1", "u1", "ListingViewed", "sports", "l1", 1L);

        assertTrue(event.isValid());
        assertTrue(event.hasCategorySlug());
        assertTrue(event.hasListing());
    }

    @Test
    void shouldRejectInvalidEvent() {
        RecommendationEventDto event = new RecommendationEventDto("", "", "", null, null, 0L);

        assertFalse(event.isValid());
        assertFalse(event.hasCategorySlug());
        assertFalse(event.hasListing());
    }

    @Test
    void shouldResolveKnownEventType() {
        assertEquals(EventType.LISTING_FAVORITED, EventType.fromValue("ListingFavorited"));
        assertNull(EventType.fromValue("Unknown"));
        assertNull(EventType.fromValue(" "));
        assertNull(EventType.fromValue(null));
    }
}

