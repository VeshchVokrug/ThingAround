package ru.veshvokrug.recommendation.consumer;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import ru.veshvokrug.recommendation.event.RecommendationEventDto;
import ru.veshvokrug.recommendation.service.ListingPopularityService;
import ru.veshvokrug.recommendation.service.UserCategoryWeightService;

import java.util.function.Consumer;

import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.verifyNoInteractions;

/**
 * Тесты для {@link RecommendationEventConsumer}.
 */
@ExtendWith(MockitoExtension.class)
class RecommendationEventConsumerTest {

    @Mock
    private UserCategoryWeightService userCategoryWeightService;

    @Mock
    private ListingPopularityService listingPopularityService;

    private RecommendationEventConsumer consumer;

    @BeforeEach
    void setUp() {
        consumer = new RecommendationEventConsumer(userCategoryWeightService, listingPopularityService);
    }

    @Test
    void shouldProcessValidEvent() {
        Consumer<RecommendationEventDto> fn = consumer.handleRecommendationEvent();
        fn.accept(new RecommendationEventDto("e1", "u1", "ListingFavorited", "sports", "l1", System.currentTimeMillis()));

        verify(userCategoryWeightService).incrementCategoryWeight("u1", "sports", 0.7);
        verify(listingPopularityService).incrementListingPopularity("sports", "l1", 5.0);
    }

    @Test
    void shouldIgnoreEventWithoutCategory() {
        Consumer<RecommendationEventDto> fn = consumer.handleRecommendationEvent();
        fn.accept(new RecommendationEventDto("e1", "u1", "ListingViewed", null, "l1", System.currentTimeMillis()));

        verifyNoInteractions(userCategoryWeightService, listingPopularityService);
    }

    @Test
    void shouldIgnoreInvalidEvent() {
        Consumer<RecommendationEventDto> fn = consumer.handleRecommendationEvent();
        fn.accept(new RecommendationEventDto("", "u1", "ListingViewed", "sports", "l1", 1L));

        verifyNoInteractions(userCategoryWeightService, listingPopularityService);
    }

    @Test
    void shouldIgnoreUnknownEventType() {
        Consumer<RecommendationEventDto> fn = consumer.handleRecommendationEvent();
        fn.accept(new RecommendationEventDto("e1", "u1", "UnknownEvent", "sports", "l1", 1L));

        verifyNoInteractions(userCategoryWeightService, listingPopularityService);
    }

    @Test
    void shouldUpdateOnlyUserWeightsWhenListingMissing() {
        Consumer<RecommendationEventDto> fn = consumer.handleRecommendationEvent();
        fn.accept(new RecommendationEventDto("e1", "u1", "SearchPerformed", "sports", null, 1L));

        verify(userCategoryWeightService).incrementCategoryWeight("u1", "sports", 0.3);
        verifyNoInteractions(listingPopularityService);
    }

    @Test
    void shouldIgnoreListingBasedEventWhenListingIsMissing() {
        Consumer<RecommendationEventDto> fn = consumer.handleRecommendationEvent();
        fn.accept(new RecommendationEventDto("e1", "u1", "ListingViewed", "sports", null, 1L));

        verifyNoInteractions(userCategoryWeightService, listingPopularityService);
    }

    @Test
    void shouldIgnoreCategoryOnlyEventWhenListingIsPresent() {
        Consumer<RecommendationEventDto> fn = consumer.handleRecommendationEvent();
        fn.accept(new RecommendationEventDto("e1", "u1", "SearchPerformed", "sports", "l1", 1L));

        verifyNoInteractions(userCategoryWeightService, listingPopularityService);
    }

    @Test
    void shouldIgnoreNullEvent() {
        Consumer<RecommendationEventDto> fn = consumer.handleRecommendationEvent();
        fn.accept(null);

        verifyNoInteractions(userCategoryWeightService, listingPopularityService);
    }
}

