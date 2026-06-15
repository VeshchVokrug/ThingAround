package ru.veshvokrug.coownership.config;

import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.DirectExchange;
import org.springframework.amqp.core.FanoutExchange;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.core.QueueBuilder;
import org.springframework.amqp.core.TopicExchange;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Топология RabbitMQ — единственного брокера сообщений сервиса.
 *
 * Исходящие:
 * - coownership-commands (fanout) — команды синхронизации листингов для
 *   catalog-service. MassTransit на стороне C# объявляет exchange для типа
 *   сообщения как durable fanout, поэтому параметры должны совпадать, иначе
 *   брокер ответит PRECONDITION_FAILED. Биндинг к очереди catalog-service
 *   создаёт сам MassTransit при старте потребителя.
 * - coownership-events (topic) — внутренние доменные события,
 *   routing key вида coownership.event.&lt;event_type&gt;.
 *
 * Входящие:
 * - rental-listing-created и booking-confirmed (fanout) — события от
 *   C#-сервисов; для каждого объявлена своя очередь с DLQ, чтобы
 *   необработанные сообщения не зацикливались.
 *
 * @author Dmitrii Marchenko
 */
@Configuration
public class RabbitMQConfig {

    public static final String INBOUND_DLX = "coownership.inbound.dlx";

    // ---------- Исходящие exchange ----------

    @Bean
    public FanoutExchange catalogCommandsExchange(
            @Value("${coownership.rabbitmq.catalog-commands-exchange:coownership-commands}") String exchange) {
        return new FanoutExchange(exchange, true, false);
    }

    @Bean
    public TopicExchange coownershipEventsExchange(
            @Value("${coownership.rabbitmq.events-exchange:coownership-events}") String exchange) {
        return new TopicExchange(exchange, true, false);
    }

    // ---------- Входящие: rental-events (MassTransit, IRentalEvents) ----------
    // RentalService публикует все события бронирования в один durable fanout exchange
    // "rental-events" через SetEntityName<IRentalEvents>. Consumer разбирает
    // MassTransit-конверт и диспатчит по полю messageType.

    @Bean
    public FanoutExchange rentalEventsExchange(
            @Value("${coownership.rabbitmq.inbound.rental-events-exchange:rental-events}")
            String exchange) {
        // Durable fanout — ровно так объявляет MassTransit; параметры должны совпадать
        return new FanoutExchange(exchange, true, false);
    }

    @Bean
    public Queue rentalEventsQueue(
            @Value("${coownership.rabbitmq.inbound.rental-events-queue:coownership.rental-events}")
            String queue) {
        return deadLetteredQueue(queue);
    }

    @Bean
    public Binding rentalEventsBinding(Queue rentalEventsQueue, FanoutExchange rentalEventsExchange) {
        return BindingBuilder.bind(rentalEventsQueue).to(rentalEventsExchange);
    }

    @Bean
    public Queue rentalEventsDeadLetterQueue(Queue rentalEventsQueue) {
        return QueueBuilder.durable(dlqName(rentalEventsQueue)).build();
    }

    @Bean
    public Binding rentalEventsDeadLetterBinding(Queue rentalEventsDeadLetterQueue,
                                                 DirectExchange inboundDeadLetterExchange) {
        return BindingBuilder.bind(rentalEventsDeadLetterQueue)
                .to(inboundDeadLetterExchange)
                .with(rentalEventsDeadLetterQueue.getName());
    }

    // ---------- Общий DLX входящих очередей ----------

    @Bean
    public DirectExchange inboundDeadLetterExchange() {
        return new DirectExchange(INBOUND_DLX, true, false);
    }

    /**
     * Durable-очередь, отправляющая отклонённые сообщения в DLQ
     * с routing key, равным имени DLQ.
     */
    private Queue deadLetteredQueue(String queue) {
        return QueueBuilder.durable(queue)
                .deadLetterExchange(INBOUND_DLX)
                .deadLetterRoutingKey(queue + ".dlq")
                .build();
    }

    private String dlqName(Queue queue) {
        return queue.getName() + ".dlq";
    }
}
