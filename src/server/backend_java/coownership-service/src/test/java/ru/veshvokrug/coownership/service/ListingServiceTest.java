package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.input.dto.ShareApplicationCreateRequestDto;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.OwnershipShare;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.output.repository.CoownershipListingRepository;
import ru.veshvokrug.coownership.output.repository.OwnershipShareRepository;
import ru.veshvokrug.coownership.output.repository.ShareApplicationRepository;

import java.math.BigDecimal;
import java.time.Clock;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;

/**
 * Unit-тесты бизнес-логики создания листинга в сервисе.
 * <p>
 * Проверяются выбор дедлайна и корректный перенос полей из запроса в сущность.
 */
@ExtendWith(MockitoExtension.class)
class ListingServiceTest {

    @Mock
    private CoownershipListingRepository coownershipListingRepository;

    @Mock
    private OwnershipShareRepository ownershipShareRepository;

    @Mock
    private ShareApplicationRepository shareApplicationRepository;

    @Mock
    private ShareApplicationEventPublisher shareApplicationEventPublisher;

    @Mock
    private ShareApplicationNotificationService notificationService;

    @Mock
    private ShareApplicationValidator shareApplicationValidator;

    @Mock
    private PeriodLifecycleService periodLifecycleService;

    private ListingService newService(Clock clock) {
        return new ListingService(
                coownershipListingRepository,
                ownershipShareRepository,
                shareApplicationRepository,
                shareApplicationEventPublisher,
                notificationService,
                shareApplicationValidator,
                periodLifecycleService,
                clock
        );
    }

    @Test
    void createListingUsesProvidedFundingDeadline() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = newService(clock);

        UUID catalogListingId = UUID.randomUUID();
        CoownershipListing saved = new CoownershipListing();
        saved.setId(UUID.randomUUID());
        saved.setCatalogListingId(catalogListingId);
        saved.setTotalShares(10);
        saved.setFundingDeadline(LocalDate.of(2026, 7, 1));
        when(coownershipListingRepository
                .findByCatalogListingId(catalogListingId))
                .thenReturn(java.util.Optional.empty());
        when(coownershipListingRepository.save(any(CoownershipListing.class))).thenReturn(saved);

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                catalogListingId,
                new BigDecimal("150000.00"),
                UUID.randomUUID(),
                10,
                LocalDate.of(2026, 7, 1)
        );

        service.createListing(request);

        ArgumentCaptor<CoownershipListing> captor = ArgumentCaptor.forClass(CoownershipListing.class);
        verify(coownershipListingRepository).save(captor.capture());
        assertThat(captor.getValue().getFundingDeadline()).isEqualTo(LocalDate.of(2026, 7, 1));
        verify(ownershipShareRepository).saveAll(anyList());
    }

    @Test
    void createListingDistributesWholeHundredAcrossShares() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = newService(clock);

        UUID catalogListingId = UUID.randomUUID();
        when(coownershipListingRepository
                .findByCatalogListingId(catalogListingId))
                .thenReturn(java.util.Optional.empty());
        when(coownershipListingRepository.save(any(CoownershipListing.class))).thenAnswer(invocation -> {
            CoownershipListing listing = invocation.getArgument(0);
            listing.setId(UUID.randomUUID());
            return listing;
        });

        service.createListing(new CoownershipListingCreateRequestDto(
                catalogListingId,
                new BigDecimal("150000.00"),
                UUID.randomUUID(),
                6,
                LocalDate.of(2026, 7, 1)
        ));

        verify(ownershipShareRepository).saveAll(argThat((List<OwnershipShare> shares) -> {
            assertThat(shares).hasSize(6);
            int totalPercentage = shares.stream().mapToInt(OwnershipShare::getPercentage).sum();
            assertThat(totalPercentage).isEqualTo(100);
            return true;
        }));
    }

    @Test
    void createListingRejectsInvalidTotalShares() {
        ListingService service = newService(Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC));

        assertThatThrownBy(() -> service.createListing(new CoownershipListingCreateRequestDto(
                UUID.randomUUID(),
                new BigDecimal("150000.00"),
                UUID.randomUUID(),
                invalidShares(),
                null
        )))
                .isInstanceOf(ServiceException.class)
                .hasMessageContaining("Количество долей должно быть от 2 до 10");
    }

    @Test
    void createListingUsesDefaultFundingDeadlineWhenMissing() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = newService(clock);

        UUID catalogListingId = UUID.randomUUID();
        when(coownershipListingRepository
                .findByCatalogListingId(catalogListingId))
                .thenReturn(java.util.Optional.empty());
        when(coownershipListingRepository.save(any(CoownershipListing.class))).thenAnswer(invocation -> {
            CoownershipListing listing = invocation.getArgument(0);
            listing.setId(UUID.randomUUID());
            return listing;
        });

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                catalogListingId,
                new BigDecimal("150000.00"),
                UUID.randomUUID(),
                10,
                null
        );

        CoownershipListing result = service.createListing(request);

        assertThat(result.getFundingDeadline()).isEqualTo(LocalDate.of(2026, 7, 18));
    }

    @Test
    void createListingMapsInputFieldsToEntity() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = newService(clock);

        UUID ownerId = UUID.randomUUID();
        UUID catalogListingId = UUID.randomUUID();
        when(coownershipListingRepository
                .findByCatalogListingId(catalogListingId))
                .thenReturn(java.util.Optional.empty());
        when(coownershipListingRepository.save(any(CoownershipListing.class))).thenAnswer(invocation -> {
            CoownershipListing listing = invocation.getArgument(0);
            listing.setId(UUID.randomUUID());
            return listing;
        });

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                catalogListingId,
                new BigDecimal("150000.00"),
                ownerId,
                8,
                null
        );

        CoownershipListing result = service.createListing(request);

        assertThat(result.getCatalogListingId()).isEqualTo(catalogListingId);
        assertThat(result.getPrice()).isEqualByComparingTo(request.price());
        assertThat(result.getOwnerId()).isEqualTo(ownerId);
        assertThat(result.getTotalShares()).isEqualTo(8);
        assertThat(result.getFundingDeadline()).isEqualTo(LocalDate.of(2026, 7, 18));
        verify(ownershipShareRepository).saveAll(anyList());
    }

    @Test
    void createListingReturnsExistingListingForSameCatalogListingId() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = newService(clock);

        UUID catalogListingId = UUID.randomUUID();
        CoownershipListing existing = new CoownershipListing();
        existing.setId(UUID.randomUUID());
        existing.setCatalogListingId(catalogListingId);
        when(coownershipListingRepository
                .findByCatalogListingId(catalogListingId))
                .thenReturn(java.util.Optional.of(existing));

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                catalogListingId,
                new BigDecimal("150000.00"),
                UUID.randomUUID(),
                10,
                null
        );

        CoownershipListing result = service.createListing(request);

        assertThat(result).isEqualTo(existing);
        verify(coownershipListingRepository, never()).save(any(CoownershipListing.class));
        verify(ownershipShareRepository, never()).saveAll(anyList());
    }

    @Test
    void createShareApplicationCreatesPendingApplicationWhenRequestedSharesAreAvailable() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = newService(clock);

        UUID listingId = UUID.randomUUID();
        UUID ownerId = UUID.randomUUID();
        UUID applicantId = UUID.randomUUID();

        CoownershipListing listing = new CoownershipListing();
        listing.setId(listingId);
        listing.setOwnerId(ownerId);
        when(coownershipListingRepository.findWithLockingById(listingId)).thenReturn(Optional.of(listing));
        when(shareApplicationRepository
                .findByListing_IdAndApplicantId(listingId, applicantId))
                .thenReturn(Optional.empty());
        when(ownershipShareRepository.countByCoownershipListing_IdAndOwnerIdIsNull(listingId)).thenReturn(4L);
        when(shareApplicationRepository.save(any(ShareApplication.class))).thenAnswer(invocation -> {
            ShareApplication app = invocation.getArgument(0);
            app.setId(UUID.randomUUID());
            return app;
        });

        ShareApplicationCreateRequestDto request = new ShareApplicationCreateRequestDto(applicantId, 3);

        ShareApplication result = service.createShareApplication(listingId, request);

        assertThat(result.getStatus()).isEqualTo(ShareApplicationStatus.PENDING);
        assertThat(result.getSharesCount()).isEqualTo(3);
        assertThat(result.getApplicantId()).isEqualTo(applicantId);
        verify(notificationService, times(1))
                .createNotification(eq(ownerId),
                        any(ShareApplication.class),
                        eq("SHARE_APPLICATION_CREATED"));
        verify(shareApplicationEventPublisher,
                times(1))
                .publish(eq("SHARE_APPLICATION_CREATED"),
                        any(ShareApplication.class));
    }

    @Test
    void createShareApplicationRejectsWhenRequestedSharesMoreThanAvailable() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = newService(clock);

        UUID listingId = UUID.randomUUID();
        UUID ownerId = UUID.randomUUID();
        UUID applicantId = UUID.randomUUID();

        CoownershipListing listing = new CoownershipListing();
        listing.setId(listingId);
        listing.setOwnerId(ownerId);
        when(coownershipListingRepository.findWithLockingById(listingId)).thenReturn(Optional.of(listing));
        when(shareApplicationRepository
                .findByListing_IdAndApplicantId(listingId, applicantId))
                .thenReturn(Optional.empty());
        when(ownershipShareRepository.countByCoownershipListing_IdAndOwnerIdIsNull(listingId)).thenReturn(2L);

        ShareApplicationCreateRequestDto request = new ShareApplicationCreateRequestDto(applicantId, 3);

        assertThatThrownBy(() -> service.createShareApplication(listingId, request))
                .isInstanceOf(ServiceException.class)
                .hasMessageContaining("Нельзя запросить больше долей");
    }

    @Test
    void createShareApplicationRejectsWhenApplicantIsOwner() {
        ListingService service = newService(Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC));

        UUID listingId = UUID.randomUUID();
        UUID ownerId = UUID.randomUUID();
        CoownershipListing listing = new CoownershipListing();
        listing.setId(listingId);
        listing.setOwnerId(ownerId);

        when(coownershipListingRepository.findWithLockingById(listingId)).thenReturn(Optional.of(listing));

        assertThatThrownBy(() -> service
                .createShareApplication(listingId,
                        new ShareApplicationCreateRequestDto(ownerId, 1)))
                .isInstanceOf(ServiceException.class)
                .hasMessageContaining("Владелец не может подать заявку");
    }

    @Test
    void rejectShareApplicationByOwnerChangesStatusToRejected() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = newService(clock);

        UUID applicationId = UUID.randomUUID();
        UUID ownerId = UUID.randomUUID();
        UUID applicantId = UUID.randomUUID();

        CoownershipListing listing = new CoownershipListing();
        listing.setId(UUID.randomUUID());
        listing.setOwnerId(ownerId);

        when(coownershipListingRepository
                .findWithWriteLockingById(listing.getId()))
                .thenReturn(Optional.of(listing));

        ShareApplication application = new ShareApplication();
        application.setId(applicationId);
        application.setListing(listing);
        application.setApplicantId(applicantId);
        application.setSharesCount(1);
        application.setStatus(ShareApplicationStatus.PENDING);

        when(shareApplicationRepository
                .findWithLockingById(applicationId))
                .thenReturn(Optional.of(application));
        when(shareApplicationRepository.save(any(ShareApplication.class))).thenAnswer(invocation -> invocation.getArgument(0));

        ShareApplication result = service.rejectShareApplicationByOwner(applicationId, ownerId);

        assertThat(result.getStatus()).isEqualTo(ShareApplicationStatus.REJECTED);
        verify(notificationService, times(1))
                .createNotification(eq(applicantId), any(ShareApplication.class), eq("SHARE_APPLICATION_REJECTED"));
        verify(shareApplicationEventPublisher, times(1))
                .publish(eq("SHARE_APPLICATION_REJECTED"), any(ShareApplication.class));
    }

    @Test
    void rejectShareApplicationByOwnerRejectsAlreadyApprovedApplication() {
        ListingService service = newService(Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC));

        UUID applicationId = UUID.randomUUID();
        UUID ownerId = UUID.randomUUID();

        CoownershipListing listing = new CoownershipListing();
        listing.setId(UUID.randomUUID());
        listing.setOwnerId(ownerId);
        when(coownershipListingRepository.findWithWriteLockingById(listing.getId())).thenReturn(Optional.of(listing));

        ShareApplication application = new ShareApplication();
        application.setId(applicationId);
        application.setListing(listing);
        application.setApplicantId(UUID.randomUUID());
        application.setSharesCount(1);
        application.setStatus(ShareApplicationStatus.APPROVED);

        when(shareApplicationRepository.findWithLockingById(applicationId)).thenReturn(Optional.of(application));

        assertThatThrownBy(() -> service.rejectShareApplicationByOwner(applicationId, ownerId))
                .isInstanceOf(ServiceException.class)
                .hasMessageContaining("Одобренную заявку нельзя отклонить");
    }

    @Test
    void approveShareApplicationByOwnerRejectsWrongOwner() {
        ListingService service = newService(Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC));

        UUID applicationId = UUID.randomUUID();
        UUID correctOwnerId = UUID.randomUUID();
        UUID wrongOwnerId = UUID.randomUUID();

        CoownershipListing listing = new CoownershipListing();
        listing.setId(UUID.randomUUID());
        listing.setOwnerId(correctOwnerId);

        ShareApplication application = new ShareApplication();
        application.setId(applicationId);
        application.setListing(listing);
        application.setApplicantId(UUID.randomUUID());
        application.setSharesCount(1);
        application.setStatus(ShareApplicationStatus.PENDING);

        when(shareApplicationRepository.findWithLockingById(applicationId)).thenReturn(Optional.of(application));
        when(coownershipListingRepository.findWithWriteLockingById(listing.getId())).thenReturn(Optional.of(listing));
        doThrow(ServiceException.forbidden("Подтверждать заявку может только владелец листинга"))
                .when(shareApplicationValidator)
                .validateOwnerCanApprove(listing, wrongOwnerId);

        assertThatThrownBy(() -> service.approveShareApplicationByOwner(applicationId, wrongOwnerId))
                .isInstanceOf(ServiceException.class)
                .hasMessageContaining("Подтверждать заявку может только владелец листинга");
    }

    @Test
    void approveShareApplicationByOwnerFillsListingLocksSharesAndTriggersPeriodLifecycle() {
        ListingService service = newService(Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC));

        UUID listingId = UUID.randomUUID();
        UUID ownerId = UUID.randomUUID();
        UUID applicantId = UUID.randomUUID();
        UUID applicationId = UUID.randomUUID();

        CoownershipListing listing = new CoownershipListing();
        listing.setId(listingId);
        listing.setOwnerId(ownerId);
        listing.setTotalShares(4);
        listing.setFilledShares(2);

        ShareApplication application = new ShareApplication();
        application.setId(applicationId);
        application.setListing(listing);
        application.setApplicantId(applicantId);
        application.setSharesCount(2);
        application.setStatus(ShareApplicationStatus.PENDING);

        OwnershipShare share1 = new OwnershipShare();
        share1.setOwnerId(null);
        share1.setLocked(false);
        OwnershipShare share2 = new OwnershipShare();
        share2.setOwnerId(null);
        share2.setLocked(false);
        OwnershipShare share3 = new OwnershipShare();
        share3.setOwnerId(applicantId);
        share3.setLocked(false);
        OwnershipShare share4 = new OwnershipShare();
        share4.setOwnerId(UUID.randomUUID());
        share4.setLocked(false);

        when(shareApplicationRepository.findWithLockingById(applicationId)).thenReturn(Optional.of(application));
        when(coownershipListingRepository.findWithWriteLockingById(listingId)).thenReturn(Optional.of(listing));
        when(ownershipShareRepository.countByCoownershipListing_IdAndOwnerIdIsNull(listingId)).thenReturn(2L);
        when(ownershipShareRepository.findFreeSharesForUpdate(eq(listingId), any())).thenReturn(List.of(share1, share2));
        when(ownershipShareRepository
                .findByCoownershipListing_Id(listingId))
                .thenReturn(List.of(share1, share2, share3, share4));
        when(shareApplicationRepository.save(any(ShareApplication.class)))
                .thenAnswer(invocation -> invocation.getArgument(0));

        ShareApplication result = service.approveShareApplicationByOwner(applicationId, ownerId);

        assertThat(result.getStatus()).isEqualTo(ShareApplicationStatus.APPROVED);
        assertThat(listing.getFilledShares()).isEqualTo(4);
        assertThat(listing.getStatus()).isEqualTo(ru.veshvokrug.coownership.model.CoownershipStatus.FILLED);
        assertThat(share1.getOwnerId()).isEqualTo(applicantId);
        assertThat(share2.getOwnerId()).isEqualTo(applicantId);
        assertThat(share1.isLocked()).isTrue();
        assertThat(share2.isLocked()).isTrue();
        assertThat(share3.isLocked()).isTrue();
        assertThat(share4.isLocked()).isTrue();

        verify(periodLifecycleService).triggerFilledOut(listing);
        verify(notificationService).createNotification(applicantId, result, "SHARE_APPLICATION_APPROVED");
        verify(shareApplicationEventPublisher).publish("SHARE_APPLICATION_APPROVED", result);
    }

    @Test
    void createShareApplicationRejectsZeroSharesCount() {
        ListingService service = newService(Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC));
        int invalidSharesCount = zeroSharesCount();

        assertThatThrownBy(() -> service.createShareApplication(
                UUID.randomUUID(),
                new ShareApplicationCreateRequestDto(UUID.randomUUID(), invalidSharesCount)
        ))
                .isInstanceOf(ServiceException.class)
                .hasMessageContaining("Количество долей должно быть больше 0");
    }

    @Test
    void rejectShareApplicationByOwnerReturnsSameApplicationWhenAlreadyRejected() {
        ListingService service = newService(Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC));

        UUID ownerId = UUID.randomUUID();
        UUID applicationId = UUID.randomUUID();

        CoownershipListing listing = new CoownershipListing();
        listing.setId(UUID.randomUUID());
        listing.setOwnerId(ownerId);

        ShareApplication application = new ShareApplication();
        application.setId(applicationId);
        application.setListing(listing);
        application.setApplicantId(UUID.randomUUID());
        application.setSharesCount(1);
        application.setStatus(ShareApplicationStatus.REJECTED);

        when(shareApplicationRepository.findWithLockingById(applicationId)).thenReturn(Optional.of(application));
        when(coownershipListingRepository.findWithWriteLockingById(listing.getId())).thenReturn(Optional.of(listing));

        ShareApplication result = service.rejectShareApplicationByOwner(applicationId, ownerId);

        assertThat(result).isSameAs(application);
        verify(shareApplicationRepository, never()).save(any(ShareApplication.class));
        verify(notificationService, never()).createNotification(any(), any(), anyString());
        verify(shareApplicationEventPublisher, never()).publish(anyString(), any(ShareApplication.class));
    }

    @Test
    void getOwnerNotificationsDelegatesToNotificationService() {
        UUID ownerId = UUID.randomUUID();
        ListingService service = newService(Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC));

        service.getOwnerNotifications(ownerId);

        verify(notificationService, times(1)).getNotifications(ownerId);
    }

    private int invalidShares() {
        return 1;
    }

    private int zeroSharesCount() {
        return 0;
    }
}
