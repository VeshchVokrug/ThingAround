package ru.veshvokrug.coownership.service;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import ru.veshvokrug.coownership.model.entity.ProcessedEvent;
import ru.veshvokrug.coownership.output.repository.ProcessedEventRepository;

import java.time.Clock;
import java.time.Instant;
import java.util.UUID;

/**
 * Сервис идемпотентной обработки входящих событий.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Service
public class InboundEventIdempotencyService {
	private static final Logger log = LoggerFactory.getLogger(InboundEventIdempotencyService.class);

	private final ProcessedEventRepository processedEventRepository;
	private final Clock clock;

	public InboundEventIdempotencyService(ProcessedEventRepository processedEventRepository, Clock clock) {
		this.processedEventRepository = processedEventRepository;
		this.clock = clock;
	}

	@Transactional
	public void executeOnce(UUID eventId, String consumerName, Runnable action) {
		if (processedEventRepository.existsByEventIdAndConsumerName(eventId, consumerName)) {
			return;
		}

		action.run();

		ProcessedEvent processedEvent = new ProcessedEvent();
		processedEvent.setEventId(eventId);
		processedEvent.setConsumerName(consumerName);
		processedEvent.setProcessedAt(Instant.now(clock));
		try {
			processedEventRepository.saveAndFlush(processedEvent);
		} catch (DataIntegrityViolationException duplicate) {
			log.debug("Duplicate event ignored: eventId={}, consumer={}", eventId, consumerName);
		}
	}
}

