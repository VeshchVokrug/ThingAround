package ru.veshvokrug.coownership.output.publisher;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.stereotype.Component;
import ru.veshvokrug.coownership.model.OutboxDestination;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.OutboxMessage;
import ru.veshvokrug.coownership.output.catalog.CoownershipListingAction;
import ru.veshvokrug.coownership.output.catalog.CoownershipListingMessage;
import ru.veshvokrug.coownership.output.repository.OutboxMessageRepository;
import ru.veshvokrug.coownership.service.CatalogListingSyncPublisher;

/**
 * Output-адаптер синхронизации с каталогом через outbox.
 * Сообщение пишется в той же транзакции, что и бизнес-операция,
 * а реальная отправка в RabbitMQ выполняется OutboxRelayService.
 *
 * @author Dmitrii Marchenko
 */
@Component
public class OutboxCatalogListingSyncPublisher implements CatalogListingSyncPublisher {
    static final String EVENT_TYPE_PREFIX = "CATALOG_LISTING_";

    private final OutboxMessageRepository outboxMessageRepository;
    private final ObjectMapper objectMapper;

    public OutboxCatalogListingSyncPublisher(OutboxMessageRepository outboxMessageRepository,
                                             ObjectMapper objectMapper) {
        this.outboxMessageRepository = outboxMessageRepository;
        this.objectMapper = objectMapper;
    }

    @Override
    public void publish(CoownershipListingAction action, CoownershipListing listing) {
        OutboxMessage message = new OutboxMessage();
        message.setEventType(EVENT_TYPE_PREFIX + action.name());
        message.setDestination(OutboxDestination.CATALOG_RABBITMQ);
        message.setPayload(serialize(CoownershipListingMessage.from(action, listing)));
        outboxMessageRepository.save(message);
    }

    private String serialize(CoownershipListingMessage message) {
        try {
            return objectMapper.writeValueAsString(message);
        } catch (JsonProcessingException e) {
            throw new IllegalStateException("Не удалось сериализовать сообщение синхронизации с каталогом", e);
        }
    }
}
