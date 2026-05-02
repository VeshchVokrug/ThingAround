package ru.veshvokrug.coownership.output.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import ru.veshvokrug.coownership.model.entity.ProcessedEvent;

import java.util.UUID;

/**
 * Репозиторий idempotency-реестра обработанных входящих событий.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
public interface ProcessedEventRepository extends JpaRepository<ProcessedEvent, UUID> {
	boolean existsByEventIdAndConsumerName(UUID eventId, String consumerName);
}
