package ru.veshvokrug.coownership.output.publisher;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.json.JsonMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import ru.veshvokrug.coownership.model.OutboxDestination;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.OutboxMessage;
import ru.veshvokrug.coownership.output.catalog.CoownershipListingAction;
import ru.veshvokrug.coownership.output.repository.OutboxMessageRepository;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;
import java.util.UUID;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.Mockito.verify;

/**
 * Тесты для {@link OutboxCatalogListingSyncPublisher}.
 */
@ExtendWith(MockitoExtension.class)
class OutboxCatalogListingSyncPublisherTest {

    @Mock
    private OutboxMessageRepository outboxMessageRepository;

    private final ObjectMapper objectMapper = JsonMapper.builder().findAndAddModules().build();

    private OutboxCatalogListingSyncPublisher publisher;

    @BeforeEach
    void setUp() {
        publisher = new OutboxCatalogListingSyncPublisher(outboxMessageRepository, objectMapper);
    }

    @Test
    void shouldSaveOutboxMessageWithCatalogDestinationAndContractFields() throws Exception {
        CoownershipListing listing = new CoownershipListing();
        listing.setId(UUID.randomUUID());
        listing.setOwnerId(UUID.randomUUID());
        listing.setCatalogListingId(UUID.randomUUID());
        listing.setPrice(new BigDecimal("1500.49"));
        listing.setTotalShares(8);
        listing.setFilledShares(3);
        listing.setTitle("Shared Camera");
        listing.setDescription("Camera");
        listing.setCategorySlug("electronics");
        listing.setCity("Moscow");
        listing.setImagesUrls(List.of("https://img/1.jpg"));
        listing.setFundingDeadline(LocalDate.of(2026, 7, 1));

        publisher.publish(CoownershipListingAction.CREATE, listing);

        ArgumentCaptor<OutboxMessage> captor = ArgumentCaptor.forClass(OutboxMessage.class);
        verify(outboxMessageRepository).save(captor.capture());
        OutboxMessage saved = captor.getValue();

        assertEquals(OutboxDestination.CATALOG_RABBITMQ, saved.getDestination());
        assertEquals("CATALOG_LISTING_CREATE", saved.getEventType());

        JsonNode payload = objectMapper.readTree(saved.getPayload());
        assertEquals(0, payload.get("action").asInt());
        assertEquals(listing.getId().toString(), payload.get("listingId").asText());
        assertEquals(listing.getOwnerId().toString(), payload.get("ownerId").asText());
        assertEquals(listing.getCatalogListingId().toString(), payload.get("catalogListingId").asText());
        assertEquals("electronics", payload.get("categorySlug").asText());
        assertEquals("Shared Camera", payload.get("title").asText());
        assertEquals("Moscow", payload.get("city").asText());
        assertEquals(1500, payload.get("sharePrice").asInt());
        assertEquals(8, payload.get("totalShares").asInt());
        assertEquals(5, payload.get("availableShares").asInt());
        assertTrue(payload.get("isActive").asBoolean());
        assertTrue(payload.has("fundingDeadline"));
        assertTrue(payload.has("titleSlug"));
    }
}
