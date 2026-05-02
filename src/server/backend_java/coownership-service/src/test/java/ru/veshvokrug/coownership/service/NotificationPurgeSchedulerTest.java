package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

/**
 * Unit-тесты scheduler'а для очистки истекших уведомлений.
 * <p>
 * Проверяют, что scheduler вызывает метод очистки в сервисе и правильно обрабатывает ошибки.
 */
@ExtendWith(MockitoExtension.class)
class NotificationPurgeSchedulerTest {

    @Mock
    private ShareApplicationNotificationService notificationService;

    @InjectMocks
    private NotificationPurgeScheduler scheduler;

    @Test
    void purgeExpiredNotificationsCallsService() {
        when(notificationService.purgeExpiredNotifications()).thenReturn(5L);

        scheduler.purgeExpiredNotifications();

        verify(notificationService).purgeExpiredNotifications();
    }

    @Test
    void purgeExpiredNotificationsHandlesExceptionsGracefully() {
        when(notificationService.purgeExpiredNotifications()).thenThrow(new RuntimeException("DB error"));

        scheduler.purgeExpiredNotifications();

        verify(notificationService).purgeExpiredNotifications();
    }
}
