package ru.veshvokrug.recommendation.config;

import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.FanoutExchange;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.core.QueueBuilder;
import org.springframework.amqp.core.TopicExchange;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.amqp.support.converter.MessageConverter;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Конфигурация RabbitMQ — единственного брокера сообщений сервиса.
 * Объявляет топологию (exchange, очереди, биндинги) и JSON-конвертер сообщений.
 *
 * @author Dmitrii Marchenko
 */
@Configuration
public class RabbitMQConfig {

    public static final String RECOMMENDATION_EVENTS_EXCHANGE = "recommendation.events.exchange";
    public static final String RECOMMENDATION_EVENTS_QUEUE = "recommendation.events.queue";
    public static final String RECOMMENDATION_EVENTS_ROUTING_KEY = "recommendation.event.*";

    public static final String RECOMMENDATION_EVENTS_DLX = "recommendation.events.dlx";
    public static final String RECOMMENDATION_EVENTS_DLQ = "recommendation.events.dlq";

    public static final String RENTAL_EVENTS_EXCHANGE = "rental-events";
    public static final String RENTAL_EVENTS_QUEUE = "recommendation.rental-events.queue";
    public static final String RENTAL_EVENTS_DLQ = "recommendation.rental-events.dlq";

    /**
     * Topic Exchange для событий рекомендаций.
     * Topic (а не Direct), потому что биндинг использует wildcard-ключ
     * {@code recommendation.event.*} — direct exchange wildcard'ы не поддерживает.
     */
    @Bean
    public TopicExchange recommendationEventsExchange() {
        return new TopicExchange(RECOMMENDATION_EVENTS_EXCHANGE, true, false);
    }

    /**
     * Очередь входящих событий. Необработанные сообщения уходят в DLQ,
     * а не зацикливаются в очереди.
     */
    @Bean
    public Queue recommendationEventsQueue() {
        return QueueBuilder.durable(RECOMMENDATION_EVENTS_QUEUE)
                .deadLetterExchange(RECOMMENDATION_EVENTS_DLX)
                .deadLetterRoutingKey(RECOMMENDATION_EVENTS_DLQ)
                .build();
    }

    @Bean
    public Binding recommendationEventsBinding(Queue recommendationEventsQueue,
                                               TopicExchange recommendationEventsExchange) {
        return BindingBuilder.bind(recommendationEventsQueue)
                .to(recommendationEventsExchange)
                .with(RECOMMENDATION_EVENTS_ROUTING_KEY);
    }

    @Bean
    public TopicExchange recommendationEventsDeadLetterExchange() {
        return new TopicExchange(RECOMMENDATION_EVENTS_DLX, true, false);
    }

    @Bean
    public Queue recommendationEventsDeadLetterQueue() {
        return QueueBuilder.durable(RECOMMENDATION_EVENTS_DLQ).build();
    }

    @Bean
    public Binding recommendationEventsDeadLetterBinding(Queue recommendationEventsDeadLetterQueue,
                                                         TopicExchange recommendationEventsDeadLetterExchange) {
        return BindingBuilder.bind(recommendationEventsDeadLetterQueue)
                .to(recommendationEventsDeadLetterExchange)
                .with(RECOMMENDATION_EVENTS_DLQ);
    }

    // ---------- Saga-события аренды от C#-сервисов (MassTransit) ----------

    /**
     * Exchange saga-событий аренды. Durable fanout — ровно так его
     * объявляет MassTransit на стороне RentalService
     * (SetEntityName для IRentalEvents), параметры должны совпадать.
     */
    @Bean
    public FanoutExchange rentalEventsExchange() {
        return new FanoutExchange(RENTAL_EVENTS_EXCHANGE, true, false);
    }

    /**
     * Очередь рекомендаций на saga-событиях аренды.
     * Сообщения здесь — MassTransit-конверты, поэтому очередь отдельная
     * от {@link #recommendationEventsQueue()} с её plain-JSON контрактом.
     */
    @Bean
    public Queue rentalEventsQueue() {
        return QueueBuilder.durable(RENTAL_EVENTS_QUEUE)
                .deadLetterExchange(RECOMMENDATION_EVENTS_DLX)
                .deadLetterRoutingKey(RENTAL_EVENTS_DLQ)
                .build();
    }

    @Bean
    public Binding rentalEventsBinding(Queue rentalEventsQueue, FanoutExchange rentalEventsExchange) {
        return BindingBuilder.bind(rentalEventsQueue).to(rentalEventsExchange);
    }

    @Bean
    public Queue rentalEventsDeadLetterQueue() {
        return QueueBuilder.durable(RENTAL_EVENTS_DLQ).build();
    }

    @Bean
    public Binding rentalEventsDeadLetterBinding(Queue rentalEventsDeadLetterQueue,
                                                 TopicExchange recommendationEventsDeadLetterExchange) {
        return BindingBuilder.bind(rentalEventsDeadLetterQueue)
                .to(recommendationEventsDeadLetterExchange)
                .with(RENTAL_EVENTS_DLQ);
    }

    /**
     * JSON-конвертер. Spring Boot автоматически подставит его
     * и в {@link org.springframework.amqp.rabbit.core.RabbitTemplate},
     * и в listener container factory — отдельные бины не нужны.
     */
    @Bean
    public MessageConverter jsonMessageConverter() {
        return new Jackson2JsonMessageConverter();
    }
}
