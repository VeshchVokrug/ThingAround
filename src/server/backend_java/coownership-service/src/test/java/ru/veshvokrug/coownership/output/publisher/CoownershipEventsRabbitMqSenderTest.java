package ru.veshvokrug.coownership.output.publisher;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.rabbit.core.RabbitTemplate;

import java.nio.charset.StandardCharsets;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.verify;

/**
 * Тесты для {@link CoownershipEventsRabbitMqSender}:
 * формат конверта и routing key должны совпадать с контрактом,
 * который раньше публиковался в Kafka-топик coownership-events.
 */
@ExtendWith(MockitoExtension.class)
class CoownershipEventsRabbitMqSenderTest {

    private static final String EXCHANGE = "coownership-events";

    @Mock
    private RabbitTemplate rabbitTemplate;

    private final ObjectMapper objectMapper = new ObjectMapper();

    private CoownershipEventsRabbitMqSender sender;

    @BeforeEach
    void setUp() {
        sender = new CoownershipEventsRabbitMqSender(rabbitTemplate, objectMapper, EXCHANGE);
    }

    @Test
    void shouldPublishEnvelopeWithEventTypeRoutingKey() throws Exception {
        UUID eventId = UUID.randomUUID();

        sender.send(eventId, "SHARE_APPLICATION_CREATED", "{\"foo\":\"bar\"}");

        ArgumentCaptor<Message> captor = ArgumentCaptor.forClass(Message.class);
        verify(rabbitTemplate).send(
                eq(EXCHANGE),
                eq("coownership.event.share_application_created"),
                captor.capture());
        Message sent = captor.getValue();

        assertEquals("application/json", sent.getMessageProperties().getContentType());
        assertEquals(eventId.toString(), sent.getMessageProperties().getMessageId());

        JsonNode envelope = objectMapper.readTree(new String(sent.getBody(), StandardCharsets.UTF_8));
        assertEquals(eventId.toString(), envelope.get("eventId").asText());
        assertEquals("SHARE_APPLICATION_CREATED", envelope.get("eventType").asText());
        assertEquals("bar", envelope.get("payload").get("foo").asText());
    }
}
