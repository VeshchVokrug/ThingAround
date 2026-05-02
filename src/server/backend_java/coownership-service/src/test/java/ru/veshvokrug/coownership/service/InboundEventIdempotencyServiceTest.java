package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import ru.veshvokrug.coownership.output.repository.ProcessedEventRepository;

import java.time.Clock;
import java.time.Instant;
import java.time.ZoneOffset;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicInteger;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class InboundEventIdempotencyServiceTest {
    @Mock
    private ProcessedEventRepository processedEventRepository;

    @Mock
    private TransactionalLockService transactionalLockService;

    private InboundEventIdempotencyService service;

    @BeforeEach
    void setUp() {
        service = new InboundEventIdempotencyService(
                processedEventRepository,
                transactionalLockService,
                Clock.fixed(Instant.parse("2026-04-27T00:00:00Z"), ZoneOffset.UTC)
        );
    }

    @Test
    void shouldSkipActionWhenEventAlreadyProcessed() {
        UUID eventId = UUID.randomUUID();
        when(processedEventRepository.existsByEventIdAndConsumerName(eventId, "consumer"))
                .thenReturn(true);

        Runnable action = mock(Runnable.class);
        service.executeOnce(eventId, "consumer", action);

        verify(transactionalLockService).lock(eq("processed-event:consumer:" + eventId));
        verify(action, never()).run();
        verify(processedEventRepository, never()).saveAndFlush(any());
    }

    @Test
    void shouldExecuteAndPersistWhenEventIsNew() {
        UUID eventId = UUID.randomUUID();
        when(processedEventRepository.existsByEventIdAndConsumerName(eventId, "consumer"))
                .thenReturn(false);

        AtomicInteger calls = new AtomicInteger();
        service.executeOnce(eventId, "consumer", calls::incrementAndGet);

        verify(transactionalLockService).lock(eq("processed-event:consumer:" + eventId));
        verify(processedEventRepository).saveAndFlush(any());
        verify(processedEventRepository).existsByEventIdAndConsumerName(eventId, "consumer");
        org.assertj.core.api.Assertions.assertThat(calls.get()).isEqualTo(1);
    }
}
