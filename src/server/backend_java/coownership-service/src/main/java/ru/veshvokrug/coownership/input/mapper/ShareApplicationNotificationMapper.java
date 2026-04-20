package ru.veshvokrug.coownership.input.mapper;

import org.springframework.stereotype.Component;
import ru.veshvokrug.coownership.input.dto.ShareApplicationNotificationDto;
import ru.veshvokrug.coownership.model.entity.ShareApplicationNotification;

/**
 * Маппинг polling-уведомлений по заявкам.
 */
@Component
public class ShareApplicationNotificationMapper {
    public ShareApplicationNotificationDto toDto(ShareApplicationNotification notification) {
        return new ShareApplicationNotificationDto(
                notification.getId(),
                notification.getRecipientId(),
                notification.getApplicationId(),
                notification.getListingId(),
                notification.getOwnerId(),
                notification.getApplicantId(),
                notification.getSharesCount(),
                notification.getApplicationStatus(),
                notification.getEventType(),
                notification.getCreatedAt(),
                notification.getExpiresAt()
        );
    }
}
