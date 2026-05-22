package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.model.entity.ShareApplicationNotification;
import ru.veshvokrug.coownership.output.repository.ShareApplicationNotificationRepository;

import java.math.BigDecimal;
import java.time.Clock;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.List;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

/**
 * Unit-тесты сервиса уведомлений.
 * <p>
 * Покрывают:
 * - Создание уведомлений с корректным expiresAt
 * - Получение только активных уведомлений
 * - Удаление уведомлений старше storage-retention дней
 * - Разные значения retention периода
 */
@ExtendWith(MockitoExtension.class)
class ShareApplicationNotificationServiceTest {

    private static final Clock FIXED_CLOCK = Clock.fixed(Instant.parse("2026-04-20T10:00:00Z"), ZoneOffset.UTC);
    private static final Instant FIXED_NOW = Instant.parse("2026-04-20T10:00:00Z");
    private static final Instant NOTIFICATION_EXPIRES = Instant.parse("2026-04-27T10:00:00Z");
    private static final int STORAGE_RETENTION_DAYS = 30;

    @Mock
    private ShareApplicationNotificationRepository notificationRepository;

    @Mock
    private ShareApplicationNotificationBuilder notificationBuilder;

    @Test
    void createNotificationBuildsDelegates() {
        ShareApplicationNotificationService service = new ShareApplicationNotificationService(
                notificationRepository, notificationBuilder, FIXED_CLOCK, STORAGE_RETENTION_DAYS);
        ShareApplication application = buildShareApplication();
        UUID recipientId = UUID.randomUUID();
        String eventType = "SHARE_APPLICATION_CREATED";

        ShareApplicationNotification builtNotification = new ShareApplicationNotification();
        builtNotification.setEventType(eventType);
        builtNotification.setExpiresAt(NOTIFICATION_EXPIRES);

        when(notificationBuilder.build(recipientId, application, eventType, NOTIFICATION_EXPIRES))
                .thenReturn(builtNotification);
        when(notificationRepository.save(builtNotification))
                .thenReturn(builtNotification);

        ShareApplicationNotification result = service.createNotification(recipientId, application, eventType);

        assertThat(result)
                .isEqualTo(builtNotification)
                .hasFieldOrPropertyWithValue("eventType", eventType)
                .hasFieldOrPropertyWithValue("expiresAt", NOTIFICATION_EXPIRES);

        verify(notificationBuilder).build(recipientId, application, eventType, NOTIFICATION_EXPIRES);
        verify(notificationRepository).save(builtNotification);
    }

    @Test
    void getNotificationsReturnsOnlyActiveRows() {
        ShareApplicationNotificationService service = new ShareApplicationNotificationService(
                notificationRepository, notificationBuilder, FIXED_CLOCK, STORAGE_RETENTION_DAYS);

        ShareApplicationNotification active = new ShareApplicationNotification();
        active.setRecipientId(UUID.randomUUID());
        active.setExpiresAt(NOTIFICATION_EXPIRES);

        when(notificationRepository.findTop100ByRecipientIdAndExpiresAtAfterOrderByCreatedAtDesc(
                eq(active.getRecipientId()),
                eq(FIXED_NOW)
        )).thenReturn(List.of(active));

        List<ShareApplicationNotification> notifications = service.getNotifications(active.getRecipientId());

        assertThat(notifications)
                .hasSize(1)
                .contains(active);
    }

    @ParameterizedTest
    @ValueSource(ints = {7, 30, 90})
    void purgeDeletesNotificationsBasedOnStorageRetention(int retentionDays) {
        ShareApplicationNotificationService service = new ShareApplicationNotificationService(
                notificationRepository, notificationBuilder, FIXED_CLOCK, retentionDays);

        Instant expectedCutoff = FIXED_NOW.minusSeconds((long) retentionDays * 86400);
        when(notificationRepository.deleteByCreatedAtBefore(expectedCutoff)).thenReturn(5L);

        long purged = service.purgeExpiredNotifications();

        assertThat(purged).isEqualTo(5L);
        verify(notificationRepository).deleteByCreatedAtBefore(expectedCutoff);
    }

    @Test
    void purgeHandlesZeroDeletedNotifications() {
        ShareApplicationNotificationService service = new ShareApplicationNotificationService(
                notificationRepository, notificationBuilder, FIXED_CLOCK, STORAGE_RETENTION_DAYS);

        when(notificationRepository.deleteByCreatedAtBefore(any())).thenReturn(0L);

        long purged = service.purgeExpiredNotifications();

        assertThat(purged).isZero();
    }

    @Test
    void getNotificationsReturnsEmptyListWhenNoActiveNotifications() {
        ShareApplicationNotificationService service = new ShareApplicationNotificationService(
                notificationRepository, notificationBuilder, FIXED_CLOCK, STORAGE_RETENTION_DAYS);

        UUID recipientId = UUID.randomUUID();
        when(notificationRepository.findTop100ByRecipientIdAndExpiresAtAfterOrderByCreatedAtDesc(
                eq(recipientId),
                eq(FIXED_NOW)
        )).thenReturn(List.of());

        List<ShareApplicationNotification> notifications = service.getNotifications(recipientId);

        assertThat(notifications).isEmpty();
    }


    private ShareApplication buildShareApplication() {
        ShareApplication application = new ShareApplication();
        application.setId(UUID.randomUUID());
        application.setApplicantId(UUID.randomUUID());
        application.setSharesCount(2);
        application.setStatus(ShareApplicationStatus.PENDING);
        application.setListing(buildCoownershipListing());
        return application;
    }

    private CoownershipListing buildCoownershipListing() {
        CoownershipListing listing = new CoownershipListing();
        listing.setId(UUID.randomUUID());
        listing.setOwnerId(UUID.randomUUID());
        listing.setCatalogListingId(UUID.randomUUID());
        listing.setPrice(new BigDecimal("150000.00"));
        listing.setTotalShares(4);
        listing.setFundingDeadline(LocalDate.now(ZoneOffset.UTC).plusDays(45));
        return listing;
    }
}
