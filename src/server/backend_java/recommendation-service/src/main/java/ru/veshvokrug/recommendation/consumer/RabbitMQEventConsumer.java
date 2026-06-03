package ru.veshvokrug.recommendation.consumer;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.stereotype.Service;
import ru.veshvokrug.recommendation.config.RabbitMQConfig;
import ru.veshvokrug.recommendation.event.RecommendationEventDto;

/**
 * RabbitMQ слушатель для получения событий от C# сервиса и других интеграторов.
 * События перенаправляются в Kafka для единой обработки.
 *
 * @author Dmitrii Marchenko
 */
@Service
public class RabbitMQEventConsumer {
    private static final Logger log = LoggerFactory.getLogger(RabbitMQEventConsumer.class);

    private final KafkaTemplate<String, RecommendationEventDto> kafkaTemplate;

    public RabbitMQEventConsumer(KafkaTemplate<String, RecommendationEventDto> kafkaTemplate) {
        this.kafkaTemplate = kafkaTemplate;
    }

    /**
     * Получать события из RabbitMQ и перенаправлять их в Kafka
     */
    @RabbitListener(queues = RabbitMQConfig.RECOMMENDATION_EVENTS_QUEUE)
    public void handleRecommendationEvent(RecommendationEventDto event) {
        try {
            if (event == null) {
                log.warn("Received null event from RabbitMQ");
                return;
            }

            log.debug("Received event from RabbitMQ: eventId={}, userId={}, eventType={}",
                    event.eventId(), event.userId(), event.eventType());

            // Перенаправить в Kafka для единой обработки
            kafkaTemplate.send("recommendation_events", event.userId(), event);

            log.debug("Event forwarded to Kafka: eventId={}", event.eventId());

        } catch (Exception e) {
            log.error("Error processing RabbitMQ event", e);
        }
    }
}

