package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.input.dto.ShareApplicationCreateRequestDto;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.entity.*;
import ru.veshvokrug.coownership.output.repository.*;
import ru.veshvokrug.coownership.support.PostgresTestcontainersSupport;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Интеграционные тесты бизнес-флоу совладения на реальном PostgreSQL.
 * <p>
 * Эти тесты проверяют именно связку сервис + JPA + Liquibase + блокировки,
 * поэтому здесь используются Testcontainers.
 */
@SpringBootTest
class ListingServiceIntegrationTest extends PostgresTestcontainersSupport {

    @Autowired
    private ListingService listingService;

    @Autowired
    private CoownershipListingRepository coownershipListingRepository;

    @Autowired
    private OwnershipShareRepository ownershipShareRepository;

    @Autowired
    private ShareApplicationRepository shareApplicationRepository;

    @Autowired
    private OutboxMessageRepository outboxMessageRepository;

    @Autowired
    private ShareApplicationNotificationRepository notificationRepository;

    @Autowired
    private OwnershipSlotsRepository ownershipSlotsRepository;

    @Autowired
    private PeriodRepository periodRepository;

    @BeforeEach
    void cleanDatabase() {
        notificationRepository.deleteAllInBatch();
        outboxMessageRepository.deleteAllInBatch();
        shareApplicationRepository.deleteAllInBatch();
        ownershipSlotsRepository.deleteAllInBatch();
        periodRepository.deleteAllInBatch();
        ownershipShareRepository.deleteAllInBatch();
        coownershipListingRepository.deleteAllInBatch();
    }

    @Test
    void createListingPersistsListingAndInitialFreeShares() {
        UUID catalogListingId = UUID.randomUUID();
        UUID ownerId = UUID.randomUUID();
        LocalDate deadline = LocalDate.now(ZoneOffset.UTC).plusDays(45);

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                catalogListingId,
                new BigDecimal("150000.00"),
                ownerId,
                6,
                deadline
        );

        CoownershipListing listing = listingService.createListing(request);

        assertThat(listing.getId()).isNotNull();
        assertThat(listing.getCatalogListingId()).isEqualTo(catalogListingId);
        assertThat(listing.getOwnerId()).isEqualTo(ownerId);
        assertThat(listing.getTotalShares()).isEqualTo(6);
        assertThat(listing.getFilledShares()).isZero();
        assertThat(listing.getStatus()).isEqualTo(CoownershipStatus.OPEN);
        assertThat(listing.getFundingDeadline()).isEqualTo(deadline);

        assertThat(ownershipShareRepository.countByCoownershipListing_IdAndOwnerIdIsNull(listing.getId()))
                .isEqualTo(6);
        assertThat(notificationRepository.count()).isZero();
        // Создание листинга кладёт в outbox команду синхронизации с каталогом
        List<OutboxMessage> outbox = outboxMessageRepository.findAll();
        assertThat(outbox).hasSize(1);
        assertThat(outbox.getFirst().getEventType()).isEqualTo("CATALOG_LISTING_CREATE");
        assertThat(outbox.getFirst().getDestination())
                .isEqualTo(ru.veshvokrug.coownership.model.OutboxDestination.CATALOG_RABBITMQ);
    }

    @Test
    void createShareApplicationCreatesPendingNotificationForOwnerPolling() {
        CoownershipListing listing = createListing(4);
        UUID applicantId = UUID.randomUUID();

        ShareApplicationCreateRequestDto request = new ShareApplicationCreateRequestDto(applicantId, 2);
        ShareApplication createdApplication = listingService.createShareApplication(listing.getId(), request);

        assertThat(createdApplication.getId()).isNotNull();
        assertThat(createdApplication.getListing().getId()).isEqualTo(listing.getId());
        assertThat(createdApplication.getApplicantId()).isEqualTo(applicantId);
        assertThat(createdApplication.getSharesCount()).isEqualTo(2);
        assertThat(createdApplication.getStatus()).isEqualTo(ShareApplicationStatus.PENDING);

        List<ShareApplicationNotification> ownerNotifications =
                listingService.getOwnerNotifications(listing.getOwnerId());
        assertThat(ownerNotifications).hasSize(1);
        assertThat(ownerNotifications.getFirst().getApplicationId()).isEqualTo(createdApplication.getId());
        assertThat(ownerNotifications.getFirst().getRecipientId()).isEqualTo(listing.getOwnerId());
        assertThat(ownerNotifications.getFirst().getExpiresAt()).isAfter(ownerNotifications.getFirst().getCreatedAt());

        assertThat(notificationRepository.count()).isEqualTo(1);
        OutboxMessage outboxMessage = outboxMessageRepository.findAll().stream()
                .filter(message -> "SHARE_APPLICATION_CREATED".equals(message.getEventType()))
                .findFirst()
                .orElseThrow();
        assertThat(outboxMessage.getPayload()).contains("\"sharesCount\": 2");
    }

    @Test
    void approveShareApplicationsInParallelSerializesListingUpdateAndFillsAllShares() throws Exception {
        CoownershipListing listing = createListing(6);
        UUID ownerId = listing.getOwnerId();

        ShareApplication firstApplication = listingService.createShareApplication(
                listing.getId(), new ShareApplicationCreateRequestDto(UUID.randomUUID(), 3)
        );
        ShareApplication secondApplication = listingService.createShareApplication(
                listing.getId(), new ShareApplicationCreateRequestDto(UUID.randomUUID(), 3)
        );

        ExecutorService executor = Executors.newFixedThreadPool(2);
        try {
            CompletableFuture<ShareApplication> firstFuture = CompletableFuture.supplyAsync(
                    () -> listingService.approveShareApplicationByOwner(firstApplication.getId(), ownerId),
                    executor
            );
            CompletableFuture<ShareApplication> secondFuture = CompletableFuture.supplyAsync(
                    () -> listingService.approveShareApplicationByOwner(secondApplication.getId(), ownerId),
                    executor
            );

            ShareApplication approvedFirst = firstFuture.get(20, TimeUnit.SECONDS);
            ShareApplication approvedSecond = secondFuture.get(20, TimeUnit.SECONDS);

            assertThat(approvedFirst.getStatus()).isEqualTo(ShareApplicationStatus.APPROVED);
            assertThat(approvedSecond.getStatus()).isEqualTo(ShareApplicationStatus.APPROVED);

            CoownershipListing reloadedListing = coownershipListingRepository.findById(listing.getId()).orElseThrow();
            assertThat(reloadedListing.getFilledShares()).isEqualTo(6);
            assertThat(reloadedListing.getStatus()).isEqualTo(CoownershipStatus.FILLED);

            List<OwnershipShare> assignedShares = ownershipShareRepository.findAll().stream()
                    .filter(share -> share.getOwnerId() != null)
                    .toList();
            assertThat(assignedShares).hasSize(6);
            assertThat(ownershipShareRepository.countByCoownershipListing_IdAndOwnerIdIsNull(listing.getId())).isZero();
            assertThat(notificationRepository.count()).isEqualTo(4);
            // 2 SHARE_APPLICATION_CREATED + 2 APPROVED + COOWNERSHIP_FILLED_OUT
            // + CATALOG_LISTING_CREATE + 2 CATALOG_LISTING_UPDATE
            assertThat(outboxMessageRepository.count()).isEqualTo(8);
        } finally {
            executor.shutdownNow();
        }
    }

    @Test
    void rejectShareApplicationKeepsSharesFreeAndMarksApplicationRejected() {
        CoownershipListing listing = createListing(3);
        UUID ownerId = listing.getOwnerId();
        UUID applicantId = UUID.randomUUID();

        ShareApplication application = listingService.createShareApplication(
                listing.getId(), new ShareApplicationCreateRequestDto(applicantId, 1)
        );

        ShareApplication rejected = listingService.rejectShareApplicationByOwner(application.getId(), ownerId);

        assertThat(rejected.getStatus()).isEqualTo(ShareApplicationStatus.REJECTED);
        assertThat(ownershipShareRepository.countByCoownershipListing_IdAndOwnerIdIsNull(listing.getId()))
                .isEqualTo(3);
        assertThat(notificationRepository.count()).isEqualTo(2);
        // SHARE_APPLICATION_CREATED + REJECTED + CATALOG_LISTING_CREATE
        assertThat(outboxMessageRepository.count()).isEqualTo(3);
    }

    private CoownershipListing createListing(int totalShares) {
        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                UUID.randomUUID(),
                new BigDecimal("150000.00"),
                UUID.randomUUID(),
                totalShares,
                LocalDate.now(ZoneOffset.UTC).plusDays(45)
        );
        return listingService.createListing(request);
    }
}
