package ru.veshvokrug.coownership.output.repository;

import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import ru.veshvokrug.coownership.model.entity.OutboxMessage;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

/**
 * Репозиторий сообщений outbox для последующей публикации в Kafka.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
public interface OutboxMessageRepository extends JpaRepository<OutboxMessage, UUID> {
	List<OutboxMessage> findByPublishedAtIsNullAndNextAttemptAtLessThanEqualOrderByNextAttemptAtAscIdAsc(
			Instant now,
			Pageable pageable
	);
}
