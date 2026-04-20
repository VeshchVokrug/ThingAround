package ru.veshvokrug.coownership.service;

import org.springframework.stereotype.Component;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.model.entity.ShareApplicationNotification;

import java.time.Instant;
import java.util.UUID;

/**
 * Builder для CreateNotification.
 * <p>
 * Отвечает за конструирование объекта уведомления из заявки и конфигурации.
 * Разделяет ответственность: сервис управляет lifecycle, builder строит объект.
 */
@Component
public class ShareApplicationNotificationBuilder {

    /**
     * Создает уведомление для заданной заявки.
     *
     * @param recipientId ID получателя уведомления
     * @param application ShareApplication с данными
     * @param eventType   тип события (SHARE_APPLICATION_CREATED, APPROVED, REJECTED)
     * @param expiresAt   время истечения уведомления
     * @return готовое уведомление для сохранения
     */
    public ShareApplicationNotification build(
            UUID recipientId,
            ShareApplication application,
            String eventType,
            Instant expiresAt) {

        ShareApplicationNotification notification = new ShareApplicationNotification();
        notification.setRecipientId(recipientId);
        notification.setApplicationId(application.getId());
        notification.setListingId(application.getListing().getId());
        notification.setOwnerId(application.getListing().getOwnerId());
        notification.setApplicantId(application.getApplicantId());
        notification.setSharesCount(application.getSharesCount());
        notification.setApplicationStatus(application.getStatus());
        notification.setEventType(eventType);
        notification.setExpiresAt(expiresAt);

        return notification;
    }
}
