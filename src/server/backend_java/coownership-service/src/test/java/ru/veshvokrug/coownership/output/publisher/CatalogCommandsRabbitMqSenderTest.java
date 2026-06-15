package ru.veshvokrug.coownership.output.publisher;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.json.JsonMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.rabbit.core.RabbitTemplate;

import java.nio.charset.StandardCharsets;
import java.time.Clock;
import java.time.Instant;
import java.time.ZoneOffset;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.verify;

/**
 * Тесты для {@link CatalogCommandsRabbitMqSender}:
 * проверяют, что сообщение публикуется в формате, который
 * MassTransit на стороне catalog-service сможет диспатчить.
 */
@ExtendWith(MockitoExtension.class)
class CatalogCommandsRabbitMqSenderTest {

    private static final String EXCHANGE = "coownership-commands";

    @Mock
    private RabbitTemplate rabbitTemplate;

    private final ObjectMapper objectMapper = JsonMapper.builder().findAndAddModules().build();
    private final Clock clock = Clock.fixed(Instant.parse("2026-06-10T12:00:00Z"), ZoneOffset.UTC);

    private CatalogCommandsRabbitMqSender sender;

    @BeforeEach
    void setUp() {
        sender = new CatalogCommandsRabbitMqSender(rabbitTemplate, objectMapper, clock, EXCHANGE, "localhost");
    }

    @Test
    void shouldWrapPayloadIntoMassTransitEnvelope() throws Exception {
        UUID messageId = UUID.randomUUID();
        String payload = "{\"action\":0,\"title\":\"Shared Camera\"}";

        sender.send(messageId, payload);

        ArgumentCaptor<Message> captor = ArgumentCaptor.forClass(Message.class);
        verify(rabbitTemplate).send(eq(EXCHANGE), eq(""), captor.capture());
        Message sent = captor.getValue();

        assertEquals(CatalogCommandsRabbitMqSender.MASS_TRANSIT_CONTENT_TYPE,
                sent.getMessageProperties().getContentType());
        assertEquals(messageId.toString(), sent.getMessageProperties().getMessageId());

        JsonNode envelope = objectMapper.readTree(new String(sent.getBody(), StandardCharsets.UTF_8));
        assertEquals(messageId.toString(), envelope.get("messageId").asText());
        assertEquals(CatalogCommandsRabbitMqSender.MESSAGE_TYPE_URN,
                envelope.get("messageType").get(0).asText());
        assertEquals("rabbitmq://localhost/" + EXCHANGE, envelope.get("destinationAddress").asText());
        assertEquals("2026-06-10T12:00:00Z", envelope.get("sentTime").asText());
        assertEquals(0, envelope.get("message").get("action").asInt());
        assertEquals("Shared Camera", envelope.get("message").get("title").asText());
    }
}
