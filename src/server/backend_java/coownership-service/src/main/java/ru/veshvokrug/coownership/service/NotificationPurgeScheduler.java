package ru.veshvokrug.coownership.service;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

/**
 * Scheduler для периодической очистки истекших уведомлений.
 * <p>
 * Запускает по расписанию для удаления всех уведомлений, чей `expiresAt` < now.
 * Это позволяет избежать выполнения очистки на критичных путях (polling requests).
 * <p>
 * Интервалы настраиваются через `coownership.notification.purge-interval-ms`
 * и `coownership.notification.purge-initial-delay-ms`.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Component
public class NotificationPurgeScheduler {
    private static final Logger logger = LoggerFactory.getLogger(NotificationPurgeScheduler.class);

    private final ShareApplicationNotificationService notificationService;

    public NotificationPurgeScheduler(ShareApplicationNotificationService notificationService) {
        this.notificationService = notificationService;
    }

    /**
     * Запускается по расписанию с интервалом и начальной задержкой из конфига.
     * <p>
     * Удаляет все notification'ы со сроком истечения (expiresAt) раньше текущего времени.
     */
    @Scheduled(fixedDelayString = "${coownership.notification.purge-interval-ms:3600000}",
               initialDelayString = "${coownership.notification.purge-initial-delay-ms:60000}")
    @Transactional
    public void purgeExpiredNotifications() {
        try {
            long deletedCount = notificationService.purgeExpiredNotifications();
            if (deletedCount > 0) {
                logger.info("Purged {} expired notifications", deletedCount);
            }
        } catch (Exception e) {
            logger.error("Error purging expired notifications", e);
        }
    }
}
