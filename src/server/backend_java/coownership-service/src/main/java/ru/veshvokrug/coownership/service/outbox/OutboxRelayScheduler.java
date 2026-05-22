package ru.veshvokrug.coownership.service.outbox;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

/**
 * Планировщик периодической отправки непрочитанных outbox-сообщений в Kafka.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Component
public class OutboxRelayScheduler {
	private static final Logger log = LoggerFactory.getLogger(OutboxRelayScheduler.class);

	private final OutboxRelayService outboxRelayService;

	public OutboxRelayScheduler(OutboxRelayService outboxRelayService) {
		this.outboxRelayService = outboxRelayService;
	}

	@Scheduled(fixedDelayString = "${coownership.outbox.relay-interval-ms:5000}")
	public void relay() {
		try {
			int published = outboxRelayService.publishNextBatch();
			if (published > 0) {
				log.info("Outbox relay published {} messages", published);
			}
		} catch (Exception ex) {
			log.error("Outbox relay failed", ex);
		}
	}
}
