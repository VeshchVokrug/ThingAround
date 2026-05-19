package ru.veshvokrug.coownership.input.grpc;

import com.google.protobuf.Timestamp;
import ru.veshvokrug.coownership.grpc.GetOwnerNotificationsResponse;
import ru.veshvokrug.coownership.grpc.ShareApplicationNotification;
import ru.veshvokrug.coownership.grpc.ShareApplicationResponse;
import ru.veshvokrug.coownership.model.entity.ShareApplication;

import java.time.Instant;
import java.util.List;

/**
 * Маппер доменных сущностей в gRPC response-модели.
 *
 * @author Dmitrii Marchenko 25.04.2026
 */
public final class GrpcModelMapper {
    private GrpcModelMapper() {
    }

    public static ShareApplicationResponse toShareApplicationResponse(ShareApplication application) {
        return ShareApplicationResponse.newBuilder()
                .setId(application.getId().toString())
                .setListingId(application.getListing().getId().toString())
                .setApplicantId(application.getApplicantId().toString())
                .setSharesCount(application.getSharesCount())
                .setStatus(application.getStatus().name())
                .build();
    }

    public static GetOwnerNotificationsResponse toOwnerNotificationsResponse(
            List<ru.veshvokrug.coownership.model.entity.ShareApplicationNotification> notifications) {
        GetOwnerNotificationsResponse.Builder builder = GetOwnerNotificationsResponse.newBuilder();
        for (ru.veshvokrug.coownership.model.entity.ShareApplicationNotification notification : notifications) {
            ShareApplicationNotification.Builder notificationBuilder = ShareApplicationNotification.newBuilder()
                    .setId(notification.getId().toString())
                    .setRecipientId(notification.getRecipientId().toString())
                    .setApplicationId(notification.getApplicationId().toString())
                    .setListingId(notification.getListingId().toString())
                    .setOwnerId(notification.getOwnerId().toString())
                    .setApplicantId(notification.getApplicantId().toString())
                    .setSharesCount(notification.getSharesCount())
                    .setApplicationStatus(notification.getApplicationStatus().name())
                    .setEventType(notification.getEventType())
                    .setExpiresAt(toTimestamp(notification.getExpiresAt()));

            if (notification.getCreatedAt() != null) {
                notificationBuilder.setCreatedAt(toTimestamp(notification.getCreatedAt()));
            }
            builder.addNotifications(notificationBuilder.build());
        }
        return builder.build();
    }

    private static Timestamp toTimestamp(Instant instant) {
        return Timestamp.newBuilder()
                .setSeconds(instant.getEpochSecond())
                .setNanos(instant.getNano())
                .build();
    }
}
