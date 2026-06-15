package ru.veshvokrug.coownership.output.publisher;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.databind.node.ObjectNode;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessageDeliveryMode;
import org.springframework.amqp.core.MessageProperties;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.nio.charset.StandardCharsets;
import java.time.Clock;
import java.time.Instant;
import java.util.UUID;

/**
 * Отправляет сообщения в catalog-service через RabbitMQ в формате MassTransit.
 *
 * Catalog-service потребляет сообщения через MassTransit, поэтому "сырое" тело
 * недостаточно: MassTransit ожидает JSON-конверт с полем {@code messageType}
 * (URN .NET-типа), по которому диспатчится consumer, и content-type
 * {@code application/vnd.masstransit+json}.
 *
 * @author Dmitrii Marchenko
 */
@Component
public class CatalogCommandsRabbitMqSender {
    static final String MASS_TRANSIT_CONTENT_TYPE = "application/vnd.masstransit+json";
    static final String MESSAGE_TYPE_URN = "urn:message:Core.Events:CoownershipListingMessage";

    private final RabbitTemplate rabbitTemplate;
    private final ObjectMapper objectMapper;
    private final String exchange;
    private final String destinationAddress;
    private final Clock clock;

    public CatalogCommandsRabbitMqSender(RabbitTemplate rabbitTemplate,
                                         ObjectMapper objectMapper,
                                         Clock clock,
                                         @Value("${coownership.rabbitmq.catalog-commands-exchange:coownership-commands}")
                                         String exchange,
                                         @Value("${spring.rabbitmq.host:localhost}") String rabbitHost) {
        this.rabbitTemplate = rabbitTemplate;
        // Даты обязаны сериализоваться ISO-строками ("2026-07-01",
        // "2026-06-10T12:00:00Z"), иначе System.Text.Json на стороне C#
        // не распарсит DateOnly/DateTime.
        this.objectMapper = objectMapper.copy().disable(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS);
        this.clock = clock;
        this.exchange = exchange;
        this.destinationAddress = "rabbitmq://" + rabbitHost + "/" + exchange;
    }

    /**
     * Публикует payload в exchange каталога, оборачивая его в MassTransit-конверт.
     * Exchange объявлен как fanout — routing key игнорируется.
     *
     * @param messageId   стабильный идентификатор сообщения (id outbox-записи),
     *                    переиспользуется при ретраях для дедупликации на стороне потребителя
     * @param payloadJson сериализованный {@link ru.veshvokrug.coownership.output.catalog.CoownershipListingMessage}
     */
    public void send(UUID messageId, String payloadJson) {
        try {
            byte[] body = buildEnvelope(messageId, payloadJson);

            MessageProperties properties = new MessageProperties();
            properties.setContentType(MASS_TRANSIT_CONTENT_TYPE);
            properties.setContentEncoding(StandardCharsets.UTF_8.name());
            properties.setDeliveryMode(MessageDeliveryMode.PERSISTENT);
            properties.setMessageId(messageId.toString());

            rabbitTemplate.send(exchange, "", new Message(body, properties));
        } catch (Exception e) {
            throw new IllegalStateException(
                    "Не удалось отправить сообщение " + messageId + " в exchange " + exchange, e);
        }
    }

    private byte[] buildEnvelope(UUID messageId, String payloadJson) throws Exception {
        ObjectNode envelope = objectMapper.createObjectNode();
        envelope.put("messageId", messageId.toString());
        envelope.put("conversationId", UUID.randomUUID().toString());
        envelope.put("destinationAddress", destinationAddress);
        envelope.putArray("messageType").add(MESSAGE_TYPE_URN);
        envelope.set("message", objectMapper.readTree(payloadJson));
        envelope.put("sentTime", Instant.now(clock).toString());
        envelope.set("headers", objectMapper.createObjectNode());
        return objectMapper.writeValueAsBytes(envelope);
    }
}
