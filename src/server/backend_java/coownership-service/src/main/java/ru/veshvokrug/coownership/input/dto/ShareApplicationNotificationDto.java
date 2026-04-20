package ru.veshvokrug.coownership.input.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;

import java.time.Instant;
import java.util.UUID;

/**
 * DTO для polling-уведомлений по заявкам на доли.
 */
public record ShareApplicationNotificationDto(
        @Schema(description = "ID уведомления", example = "7f000001-9daa-11fc-819d-aa91ff690000")
        UUID id,

        @Schema(description = "ID получателя уведомления", example = "55e9a858-f211-44a0-8e14-5eddaa7bbda3")
        UUID recipientId,

        @Schema(description = "ID заявки", example = "7f000001-9daa-11fc-819d-aa91ff9a0005")
        UUID applicationId,

        @Schema(description = "ID листинга", example = "7f000001-9daa-11fc-819d-aa91ff690000")
        UUID listingId,

        @Schema(description = "ID владельца листинга", example = "55e9a858-f211-44a0-8e14-5eddaa7bbda3")
        UUID ownerId,

        @Schema(description = "ID заявителя", example = "834b4c5c-67b8-41a6-b0e7-ac91a0a6d15e")
        UUID applicantId,

        @Schema(description = "Количество долей в заявке", example = "2")
        int sharesCount,

        @Schema(description = "Текущий статус заявки", example = "PENDING")
        ShareApplicationStatus applicationStatus,

        @Schema(description = "Тип уведомления", example = "SHARE_APPLICATION_CREATED")
        String eventType,

        @Schema(description = "Когда уведомление создано", example = "2026-04-20T11:08:45Z")
        Instant createdAt,

        @Schema(description = "Когда уведомление устаревает и больше не должно возвращаться в polling",
                example = "2026-04-27T11:08:45Z")
        Instant expiresAt
) {
}
