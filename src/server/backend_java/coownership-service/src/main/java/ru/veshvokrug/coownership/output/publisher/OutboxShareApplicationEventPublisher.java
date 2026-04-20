package ru.veshvokrug.coownership.output.publisher;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.stereotype.Component;
import ru.veshvokrug.coownership.model.entity.OutboxMessage;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.output.repository.OutboxMessageRepository;
import ru.veshvokrug.coownership.service.ShareApplicationEventPublisher;

import java.util.UUID;

/**
 * Output-адаптер публикации доменных событий в outbox.
 */
@Component
public class OutboxShareApplicationEventPublisher implements ShareApplicationEventPublisher {
	private final OutboxMessageRepository outboxMessageRepository;
	private final ObjectMapper objectMapper;

	public OutboxShareApplicationEventPublisher(OutboxMessageRepository outboxMessageRepository,
												ObjectMapper objectMapper) {
		this.outboxMessageRepository = outboxMessageRepository;
		this.objectMapper = objectMapper;
	}

	@Override
	public void publish(String eventType, ShareApplication shareApplication) {
		OutboxMessage message = new OutboxMessage();
		message.setEventType(eventType);
		message.setPayload(serializePayload(shareApplication));
		outboxMessageRepository.save(message);
	}

	private String serializePayload(ShareApplication shareApplication) {
		ShareApplicationEventPayload payload = new ShareApplicationEventPayload(
				shareApplication.getId(),
				shareApplication.getListing().getId(),
				shareApplication.getListing().getOwnerId(),
				shareApplication.getApplicantId(),
				shareApplication.getSharesCount(),
				shareApplication.getStatus().name()
		);
		try {
			return objectMapper.writeValueAsString(payload);
		} catch (JsonProcessingException e) {
			throw new IllegalStateException("Не удалось сериализовать payload outbox-события", e);
		}
	}

	private record ShareApplicationEventPayload(UUID applicationId,
												UUID listingId,
												UUID ownerId,
												UUID applicantId,
												int sharesCount,
												String status) {
	}
}
