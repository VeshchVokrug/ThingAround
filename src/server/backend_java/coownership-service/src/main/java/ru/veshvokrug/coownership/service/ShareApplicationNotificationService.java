package ru.veshvokrug.coownership.service;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.model.entity.ShareApplicationNotification;
import ru.veshvokrug.coownership.output.repository.ShareApplicationNotificationRepository;

import java.time.Clock;
import java.time.Duration;
import java.time.Instant;
import java.util.List;
import java.util.UUID;

/**
 * Сервис управления polling-уведомлениями об изменениях статуса заявок.
 * <p>
 * Ответственность:
 * - Создание уведомлений с управлением TTL
 * - Получение активных уведомлений для фронтенда
 * - Архивирование (удаление) старых уведомлений
 * <p>
 * Построение объекта уведомления делегируется ShareApplicationNotificationBuilder.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Service
public class ShareApplicationNotificationService {
    private static final Duration RETENTION = Duration.ofDays(7);

    private final ShareApplicationNotificationRepository notificationRepository;
    private final ShareApplicationNotificationBuilder notificationBuilder;
    private final Clock clock;
    private final Duration storageRetention;

    public ShareApplicationNotificationService(
            ShareApplicationNotificationRepository notificationRepository,
            ShareApplicationNotificationBuilder notificationBuilder,
            Clock clock,
            @Value("${coownership.notification.storage-retention-days:30}") int storageRetentionDays) {
        this.notificationRepository = notificationRepository;
        this.notificationBuilder = notificationBuilder;
        this.clock = clock;
        this.storageRetention = Duration.ofDays(storageRetentionDays);
    }

    /**
     * Создает уведомление для заявки и сохраняет его.
     *
     * @param recipientId ID получателя уведомления
     * @param application ShareApplication
     * @param eventType   тип события
     * @return сохраненное уведомление
     */
    @Transactional
    public ShareApplicationNotification createNotification(UUID recipientId,
                                                           ShareApplication application,
                                                           String eventType) {
        Instant now = Instant.now(clock);
        Instant expiresAt = now.plus(RETENTION);

        ShareApplicationNotification notification = notificationBuilder.build(
                recipientId,
                application,
                eventType,
                expiresAt
        );

        return notificationRepository.save(notification);
    }

    /**
     * Удаляет уведомления старше сконфигурированного периода хранения.
     * Используется scheduler'ом для архивирования.
     *
     * @return количество удаленных уведомлений
     */
    @Transactional
    public long purgeExpiredNotifications() {
        Instant cutoff = Instant.now(clock).minus(storageRetention);
        return notificationRepository.deleteByCreatedAtBefore(cutoff);
    }

    /**
     * Получает активные (не истекшие) уведомления для получателя.
     *
     * @param recipientId ID получателя
     * @return список из до 100 последних активных уведомлений (DESC по created_at)
     */
    @Transactional
    public List<ShareApplicationNotification> getNotifications(UUID recipientId) {
        return notificationRepository.findTop100ByRecipientIdAndExpiresAtAfterOrderByCreatedAtDesc(
                recipientId,
                Instant.now(clock)
        );
    }
}
