package ru.veshvokrug.recommendation.config;

import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.DirectExchange;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Конфигурация RabbitMQ для альтернативной интеграции с внешними сервисами.
 * Используется параллельно с Kafka для обеспечения гибкости при интеграции с C#.
 *
 * @author Dmitrii Marchenko
 */
@Configuration
public class RabbitMQConfig {

    // Очередь для получения событий от C# сервиса
    public static final String RECOMMENDATION_EVENTS_QUEUE = "recommendation.events.queue";
    public static final String RECOMMENDATION_EVENTS_EXCHANGE = "recommendation.events.exchange";
    public static final String RECOMMENDATION_EVENTS_ROUTING_KEY = "recommendation.event.*";

    // Очередь для отправки рекомендаций в C# сервис
    public static final String RECOMMENDATIONS_RESPONSE_QUEUE = "recommendations.response.queue";
    public static final String RECOMMENDATIONS_RESPONSE_EXCHANGE = "recommendations.response.exchange";
    public static final String RECOMMENDATIONS_RESPONSE_ROUTING_KEY = "recommendations.response";

    /**
     * Очередь для получения событий от интеграторов (C#, Node.js и т.д.)
     */
    @Bean
    public Queue recommendationEventsQueue() {
        return new Queue(RECOMMENDATION_EVENTS_QUEUE, true, false, false);
    }

    /**
     * Direct Exchange для маршрутизации событий
     */
    @Bean
    public DirectExchange recommendationEventsExchange() {
        return new DirectExchange(RECOMMENDATION_EVENTS_EXCHANGE, true, false);
    }

    /**
     * Binding между очередью и exchange
     */
    @Bean
    public Binding recommendationEventsBinding(Queue recommendationEventsQueue,
                                               DirectExchange recommendationEventsExchange) {
        return BindingBuilder.bind(recommendationEventsQueue)
                .to(recommendationEventsExchange)
                .with(RECOMMENDATION_EVENTS_ROUTING_KEY);
    }

    /**
     * Очередь для отправки ответов с рекомендациями
     */
    @Bean
    public Queue recommendationsResponseQueue() {
        return new Queue(RECOMMENDATIONS_RESPONSE_QUEUE, true, false, false);
    }

    /**
     * Direct Exchange для отправки ответов
     */
    @Bean
    public DirectExchange recommendationsResponseExchange() {
        return new DirectExchange(RECOMMENDATIONS_RESPONSE_EXCHANGE, true, false);
    }

    /**
     * Binding для ответов
     */
    @Bean
    public Binding recommendationsResponseBinding(Queue recommendationsResponseQueue,
                                                  DirectExchange recommendationsResponseExchange) {
        return BindingBuilder.bind(recommendationsResponseQueue)
                .to(recommendationsResponseExchange)
                .with(RECOMMENDATIONS_RESPONSE_ROUTING_KEY);
    }

    /**
     * RabbitTemplate для отправки сообщений
     */
    @Bean
    public RabbitTemplate rabbitTemplate(ConnectionFactory connectionFactory) {
        RabbitTemplate template = new RabbitTemplate(connectionFactory);
        template.setMessageConverter(new Jackson2JsonMessageConverter());
        return template;
    }
}

