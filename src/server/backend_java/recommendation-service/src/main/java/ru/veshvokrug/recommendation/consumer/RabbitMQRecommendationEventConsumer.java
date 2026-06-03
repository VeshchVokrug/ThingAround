package ru.veshvokrug.recommendation.consumer;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;
import ru.veshvokrug.recommendation.config.EventWeightsConfig;
import ru.veshvokrug.recommendation.event.RecommendationEventDto;
import ru.veshvokrug.recommendation.service.ListingPopularityService;
import ru.veshvokrug.recommendation.service.UserCategoryWeightService;

/**
 * Потребитель событий рекомендаций из RabbitMQ.
 * Альтернатива Kafka consumer для асинхронной интеграции с C# сервисами и другими микросервисами.
 * Слушает очередь `recommendation.events.queue`.
 *
 * @author Dmitrii Marchenko
 */
@Component
public class RabbitMQRecommendationEventConsumer {
    private static final Logger logger = LoggerFactory.getLogger(RabbitMQRecommendationEventConsumer.class);

    private final UserCategoryWeightService userCategoryWeightService;
    private final ListingPopularityService listingPopularityService;

    public RabbitMQRecommendationEventConsumer(
            UserCategoryWeightService userCategoryWeightService,
            ListingPopularityService listingPopularityService) {
        this.userCategoryWeightService = userCategoryWeightService;
        this.listingPopularityService = listingPopularityService;
    }

    /**
     * Обработать событие рекомендации из RabbitMQ.
     * Слушает очередь `recommendation.events.queue`.
     *
     * @param event событие из RabbitMQ
     */
    @RabbitListener(queues = "recommendation.events.queue")
    public void handleRecommendationEvent(RecommendationEventDto event) {
        try {
            logger.debug("RabbitMQ: Received event - eventId={}, userId={}, eventType={}",
                    event.getEventId(), event.getUserId(), event.getEventType());

            // Валидация
            if (event == null || event.getUserId() == null || event.getUserId().isBlank()) {
                logger.warn("RabbitMQ: Event userId is null or empty, skipping event");
                return;
            }

            if (event.getEventType() == null || event.getEventType().isBlank()) {
                logger.warn("RabbitMQ: Event type is null or empty, skipping event");
                return;
            }

            if (event.getCategorySlug() == null || event.getCategorySlug().isBlank()) {
                logger.warn("RabbitMQ: Category slug is null or empty for event {}, skipping", event.getEventId());
                return;
            }

            // Получить вес события из конфигурации
            Double userInterestWeight = EventWeightsConfig.USER_INTEREST_WEIGHTS.get(event.getEventType());
            if (userInterestWeight == null || userInterestWeight == 0.0) {
                logger.debug("RabbitMQ: No user interest weight for event type {}, skipping",
                        event.getEventType());
                return;
            }

            // Обновить вес категории пользователя
            userCategoryWeightService.updateUserCategoryWeight(
                    event.getUserId(),
                    event.getCategorySlug(),
                    userInterestWeight
            );
            logger.debug("RabbitMQ: Updated user category weight - userId={}, category={}, weight={}",
                    event.getUserId(), event.getCategorySlug(), userInterestWeight);

            // Если есть listingId, обновить популярность объявления
            if (event.getListingId() != null && !event.getListingId().isBlank()) {
                Double popularityWeight = EventWeightsConfig.LISTING_POPULARITY_WEIGHTS.get(event.getEventType());
                if (popularityWeight != null && popularityWeight != 0.0) {
                    listingPopularityService.updateListingPopularity(
                            event.getCategorySlug(),
                            event.getListingId(),
                            popularityWeight
                    );
                    logger.debug("RabbitMQ: Updated listing popularity - category={}, listing={}, weight={}",
                            event.getCategorySlug(), event.getListingId(), popularityWeight);
                }
            }

            logger.debug("RabbitMQ: Successfully processed event - eventId={}", event.getEventId());

        } catch (Exception e) {
            logger.error("RabbitMQ: Error processing event", e);
        }
    }
}

