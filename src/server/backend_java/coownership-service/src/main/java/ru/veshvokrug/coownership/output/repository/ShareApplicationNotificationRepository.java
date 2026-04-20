package ru.veshvokrug.coownership.output.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import ru.veshvokrug.coownership.model.entity.ShareApplicationNotification;

import java.time.Instant;
import java.util.List;
import java.util.UUID;

public interface ShareApplicationNotificationRepository extends JpaRepository<ShareApplicationNotification, UUID> {
    List<ShareApplicationNotification> findTop100ByRecipientIdAndExpiresAtAfterOrderByCreatedAtDesc(
            UUID recipientId,
            Instant expiresAt
    );

    @Modifying
    long deleteByExpiresAtBefore(Instant cutoff);

    @Modifying
    long deleteByCreatedAtBefore(Instant cutoff);
}
