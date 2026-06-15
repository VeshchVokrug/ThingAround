package ru.veshvokrug.coownership.service.outbox;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import ru.veshvokrug.coownership.model.entity.OutboxMessage;
import ru.veshvokrug.coownership.output.repository.OutboxMessageRepository;

/**
 * Сервис унифицированной записи исходящих доменных событий в outbox.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Service
public class OutboxEventService {
    private final OutboxMessageRepository outboxMessageRepository;
    private final ObjectMapper objectMapper;

    public OutboxEventService(OutboxMessageRepository outboxMessageRepository, ObjectMapper objectMapper) {
        this.outboxMessageRepository = outboxMessageRepository;
        this.objectMapper = objectMapper;
    }

    @Transactional
    public void save(String eventType, Object payload) {
        OutboxMessage outboxMessage = new OutboxMessage();
        outboxMessage.setEventType(eventType);
        outboxMessage.setPayload(serialize(payload));
        outboxMessageRepository.save(outboxMessage);
    }

    private String serialize(Object payload) {
        try {
            return objectMapper.writeValueAsString(payload);
        } catch (JsonProcessingException e) {
            throw new IllegalStateException("Не удалось сериализовать payload outbox-события", e);
        }
    }
}
