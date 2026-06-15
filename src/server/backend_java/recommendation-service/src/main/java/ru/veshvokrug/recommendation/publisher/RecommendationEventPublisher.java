package ru.veshvokrug.recommendation.publisher;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.stereotype.Component;
import ru.veshvokrug.recommendation.config.RabbitMQConfig;
import ru.veshvokrug.recommendation.event.RecommendationEventDto;

import java.util.Locale;

/**
 * Публикует события рекомендаций в RabbitMQ.
 * Единственная точка отправки: инкапсулирует exchange и routing key,
 * чтобы контроллеры и gRPC-сервис не знали о деталях брокера.
 *
 * @author Dmitrii Marchenko
 */
@Component
public class RecommendationEventPublisher {
    private static final Logger log = LoggerFactory.getLogger(RecommendationEventPublisher.class);

    private static final String ROUTING_KEY_PREFIX = "recommendation.event.";

    private final RabbitTemplate rabbitTemplate;

    public RecommendationEventPublisher(RabbitTemplate rabbitTemplate) {
        this.rabbitTemplate = rabbitTemplate;
    }

    /**
     * Отправляет событие в exchange {@code recommendation.events.exchange}
     * с ключом вида {@code recommendation.event.<eventType>}.
     *
     * @param event валидное событие рекомендаций
     */
    public void publish(RecommendationEventDto event) {
        String routingKey = ROUTING_KEY_PREFIX + event.eventType().toLowerCase(Locale.ROOT);

        rabbitTemplate.convertAndSend(
                RabbitMQConfig.RECOMMENDATION_EVENTS_EXCHANGE,
                routingKey,
                event);

        log.debug("Событие опубликовано в RabbitMQ: eventId={}, routingKey={}",
                event.eventId(), routingKey);
    }
}
