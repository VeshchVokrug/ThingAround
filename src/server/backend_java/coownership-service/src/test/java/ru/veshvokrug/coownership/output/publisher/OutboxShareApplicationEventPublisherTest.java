package ru.veshvokrug.coownership.output.publisher;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.OutboxMessage;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.output.repository.OutboxMessageRepository;

import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class OutboxShareApplicationEventPublisherTest {

    @Mock
    private OutboxMessageRepository outboxMessageRepository;

    @Mock
    private ObjectMapper objectMapper;

    @Test
    void publishSavesOutboxMessageWithSerializedPayload() {
        OutboxShareApplicationEventPublisher publisher =
                new OutboxShareApplicationEventPublisher(outboxMessageRepository, new ObjectMapper());

        UUID applicationId = UUID.randomUUID();
        UUID listingId = UUID.randomUUID();
        UUID ownerId = UUID.randomUUID();
        UUID applicantId = UUID.randomUUID();

        CoownershipListing listing = new CoownershipListing();
        listing.setId(listingId);
        listing.setOwnerId(ownerId);

        ShareApplication shareApplication = new ShareApplication();
        shareApplication.setId(applicationId);
        shareApplication.setListing(listing);
        shareApplication.setApplicantId(applicantId);
        shareApplication.setSharesCount(2);
        shareApplication.setStatus(ShareApplicationStatus.PENDING);

        publisher.publish("SHARE_APPLICATION_CREATED", shareApplication);

        ArgumentCaptor<OutboxMessage> captor = ArgumentCaptor.forClass(OutboxMessage.class);
        verify(outboxMessageRepository).save(captor.capture());

        OutboxMessage outboxMessage = captor.getValue();
        assertThat(outboxMessage.getEventType()).isEqualTo("SHARE_APPLICATION_CREATED");
        assertThat(outboxMessage.getPayload()).contains(applicationId.toString());
        assertThat(outboxMessage.getPayload()).contains(listingId.toString());
        assertThat(outboxMessage.getPayload()).contains(ownerId.toString());
        assertThat(outboxMessage.getPayload()).contains(applicantId.toString());
        assertThat(outboxMessage.getPayload()).contains("\"sharesCount\":2");
        assertThat(outboxMessage.getPayload()).contains("\"status\":\"PENDING\"");
    }

    @Test
    void publishThrowsIllegalStateExceptionWhenSerializationFails() throws Exception {
        OutboxShareApplicationEventPublisher publisher =
                new OutboxShareApplicationEventPublisher(outboxMessageRepository, objectMapper);

        CoownershipListing listing = new CoownershipListing();
        listing.setId(UUID.randomUUID());
        listing.setOwnerId(UUID.randomUUID());

        ShareApplication shareApplication = new ShareApplication();
        shareApplication.setId(UUID.randomUUID());
        shareApplication.setListing(listing);
        shareApplication.setApplicantId(UUID.randomUUID());
        shareApplication.setSharesCount(1);
        shareApplication.setStatus(ShareApplicationStatus.REJECTED);

        doThrow(new JsonProcessingException("boom") {
        }).when(objectMapper).writeValueAsString(any());

        assertThatThrownBy(() -> publisher.publish("SHARE_APPLICATION_REJECTED", shareApplication))
                .isInstanceOf(IllegalStateException.class)
                .hasMessageContaining("Не удалось сериализовать payload outbox-события");
    }
}
