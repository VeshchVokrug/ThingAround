package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.model.entity.ShareApplicationNotification;

import java.math.BigDecimal;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Unit-тесты для ShareApplicationNotificationBuilder.
 * <p>
 * Проверяют корректное построение объекта уведомления со всеми полями.
 */
@ExtendWith(MockitoExtension.class)
class ShareApplicationNotificationBuilderTest {

    @InjectMocks
    private ShareApplicationNotificationBuilder builder;

    @Test
    void buildCreatesNotificationWithAllFieldsMapped() {
        // Arrange
        UUID recipientId = UUID.randomUUID();
        ShareApplication application = buildShareApplication();
        String eventType = "SHARE_APPLICATION_APPROVED";
        Instant expiresAt = Instant.now().plusSeconds(604800); // +7 days

        // Act
        ShareApplicationNotification notification = builder.build(
                recipientId,
                application,
                eventType,
                expiresAt
        );

        // Verify all fields are mapped correctly
        assertThat(notification)
                .hasFieldOrPropertyWithValue("recipientId", recipientId)
                .hasFieldOrPropertyWithValue("applicationId", application.getId())
                .hasFieldOrPropertyWithValue("listingId", application.getListing().getId())
                .hasFieldOrPropertyWithValue("ownerId", application.getListing().getOwnerId())
                .hasFieldOrPropertyWithValue("applicantId", application.getApplicantId())
                .hasFieldOrPropertyWithValue("sharesCount", application.getSharesCount())
                .hasFieldOrPropertyWithValue("applicationStatus", ShareApplicationStatus.PENDING)
                .hasFieldOrPropertyWithValue("eventType", eventType)
                .hasFieldOrPropertyWithValue("expiresAt", expiresAt);
    }

    @Test
    void buildHandlesEventsCorrectly() {
        String[] eventTypes = {
                "SHARE_APPLICATION_CREATED",
                "SHARE_APPLICATION_APPROVED",
                "SHARE_APPLICATION_REJECTED"
        };

        for (String eventType : eventTypes) {
            // Act
            ShareApplicationNotification notification = builder.build(
                    UUID.randomUUID(),
                    buildShareApplication(),
                    eventType,
                    Instant.now()
            );

            // Verify
            assertThat(notification.getEventType()).isEqualTo(eventType);
        }
    }

    // ========== Test Builders ==========

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
        listing.setName("Test Listing");
        listing.setDescription("Test Description");
        listing.setPrice(new BigDecimal("150000.00"));
        listing.setTotalShares(4);
        listing.setFundingDeadline(LocalDate.now(ZoneOffset.UTC).plusDays(45));
        return listing;
    }
}
