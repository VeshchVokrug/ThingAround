package ru.veshvokrug.recommendation.consumer;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;
import ru.veshvokrug.recommendation.config.RabbitMQConfig;
import ru.veshvokrug.recommendation.event.EventType;
import ru.veshvokrug.recommendation.event.RecommendationEventDto;
import ru.veshvokrug.recommendation.service.ListingPopularityService;
import ru.veshvokrug.recommendation.service.UserCategoryWeightService;

/**
 * RabbitMQ consumer событий рекомендаций.
 * Слушает очередь {@code recommendation.events.queue},
 * обрабатывает пользовательскую активность и обновляет веса в Redis.
 *
 * @author Dmitrii Marchenko
 */
@Component
public class RecommendationEventConsumer {
    private static final Logger logger = LoggerFactory.getLogger(RecommendationEventConsumer.class);

    private final UserCategoryWeightService userCategoryWeightService;
    private final ListingPopularityService listingPopularityService;

    public RecommendationEventConsumer(
            UserCategoryWeightService userCategoryWeightService,
            ListingPopularityService listingPopularityService) {
        this.userCategoryWeightService = userCategoryWeightService;
        this.listingPopularityService = listingPopularityService;
    }

    /**
     * Точка входа для сообщений из RabbitMQ.
     * Некорректные события логируются и пропускаются (без requeue),
     * чтобы не блокировать очередь.
     */
    @RabbitListener(queues = RabbitMQConfig.RECOMMENDATION_EVENTS_QUEUE)
    public void handleRecommendationEvent(RecommendationEventDto event) {
        try {
            processEvent(event);
        } catch (Exception e) {
            logger.error("Ошибка при обработке события рекомендаций: {}", event, e);
        }
    }

    /**
     * Обрабатывает одно событие рекомендаций и обновляет веса в Redis.
     */
    private void processEvent(RecommendationEventDto event) {
        if (event == null) {
            logger.warn("Получено пустое событие");
            return;
        }

        if (!event.isValid()) {
            logger.warn("Получено некорректное событие: {}", event);
            return;
        }

        if (!event.hasCategorySlug()) {
            logger.debug("Пропускаю событие без категории: {}", event.eventId());
            return;
        }

        // Определяем тип события
        EventType eventType = EventType.fromValue(event.eventType());
        if (eventType == null) {
            logger.warn("Неизвестный тип события: {}", event.eventType());
            return;
        }

        // Контрактная валидация listingId:
        // - для событий, привязанных к объявлению, listingId обязателен;
        // - для category-only событий listingId должен быть пустым.
        if (requiresListing(eventType) && !event.hasListing()) {
            logger.warn("Пропускаю событие {} типа {}: отсутствует listingId", event.eventId(), eventType.getValue());
            return;
        }
        if (forbidsListing(eventType) && event.hasListing()) {
            logger.warn("Пропускаю событие {} типа {}: listingId должен быть null/empty",
                    event.eventId(), eventType.getValue());
            return;
        }

        try {
            // Обновляем вес интереса пользователя
            double userInterestWeight = eventType.getUserInterestWeight();
            userCategoryWeightService.incrementCategoryWeight(
                    event.userId(),
                    event.categorySlug(),
                    userInterestWeight);

            logger.debug("Обновлён интерес пользователя: userId={}, category={}, weight={}",
                    event.userId(), event.categorySlug(), userInterestWeight);

            // Обновляем популярность объявления (только для listing-based событий)
            if (event.hasListing()) {
                double listingWeight = eventType.getListingPopularityWeight();
                listingPopularityService.incrementListingPopularity(
                        event.categorySlug(),
                        event.listingId(),
                        listingWeight);

                logger.debug("Обновлена популярность объявления: category={}, listing={}, weight={}",
                        event.categorySlug(), event.listingId(), listingWeight);
            }

            logger.trace("Событие успешно обработано: {}", event.eventId());

        } catch (Exception e) {
            logger.error("Ошибка при обновлении весов для события {}", event.eventId(), e);
        }
    }

    private static boolean requiresListing(EventType eventType) {
        return !forbidsListing(eventType);
    }

    private static boolean forbidsListing(EventType eventType) {
        return eventType == EventType.SEARCH_PERFORMED || eventType == EventType.USER_CATEGORIES_UPDATED;
    }
}
