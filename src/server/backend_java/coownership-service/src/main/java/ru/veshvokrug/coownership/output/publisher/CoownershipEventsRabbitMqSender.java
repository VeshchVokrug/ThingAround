package ru.veshvokrug.coownership.output.publisher;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessageDeliveryMode;
import org.springframework.amqp.core.MessageProperties;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Component;

import java.nio.charset.StandardCharsets;
import java.util.Locale;
import java.util.UUID;

/**
 * Отправляет внутренние доменные события сервиса в topic exchange RabbitMQ.
 *
 * Формат сообщения тот же, что был в Kafka-топике coownership-events:
 * конверт {@code {"eventId": ..., "eventType": ..., "payload": {...}}},
 * поэтому контракт для потребителей не меняется — меняется только транспорт.
 * Routing key вида {@code coownership.event.<event_type>} позволяет
 * потребителям подписываться выборочно (например,
 * {@code coownership.event.share_application_*}).
 *
 * @author Dmitrii Marchenko
 */
@Component
public class CoownershipEventsRabbitMqSender {
    static final String ROUTING_KEY_PREFIX = "coownership.event.";

    private final RabbitTemplate rabbitTemplate;
    private final ObjectMapper objectMapper;
    private final String exchange;

    public CoownershipEventsRabbitMqSender(RabbitTemplate rabbitTemplate,
                                           ObjectMapper objectMapper,
                                           @Value("${coownership.rabbitmq.events-exchange:coownership-events}")
                                           String exchange) {
        this.rabbitTemplate = rabbitTemplate;
        this.objectMapper = objectMapper;
        this.exchange = exchange;
    }

    /**
     * Публикует событие в exchange coownership-events.
     *
     * @param eventId     стабильный идентификатор события (id outbox-записи),
     *                    при ретраях не меняется — потребители могут дедуплицировать
     * @param eventType   тип события, например SHARE_APPLICATION_CREATED
     * @param payloadJson сериализованный payload события
     */
    public void send(UUID eventId, String eventType, String payloadJson) {
        try {
            byte[] body = buildEnvelope(eventId, eventType, payloadJson);

            MessageProperties properties = new MessageProperties();
            properties.setContentType(MediaType.APPLICATION_JSON_VALUE);
            properties.setContentEncoding(StandardCharsets.UTF_8.name());
            properties.setDeliveryMode(MessageDeliveryMode.PERSISTENT);
            properties.setMessageId(eventId.toString());

            String routingKey = ROUTING_KEY_PREFIX + eventType.toLowerCase(Locale.ROOT);
            rabbitTemplate.send(exchange, routingKey, new Message(body, properties));
        } catch (Exception e) {
            throw new IllegalStateException(
                    "Не удалось отправить событие " + eventId + " в exchange " + exchange, e);
        }
    }

    private byte[] buildEnvelope(UUID eventId, String eventType, String payloadJson) throws Exception {
        ObjectNode envelope = objectMapper.createObjectNode();
        envelope.put("eventId", eventId.toString());
        envelope.put("eventType", eventType);
        envelope.set("payload", objectMapper.readTree(payloadJson));
        return objectMapper.writeValueAsBytes(envelope);
    }
}
