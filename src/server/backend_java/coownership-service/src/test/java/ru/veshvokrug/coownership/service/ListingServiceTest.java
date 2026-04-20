package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.web.server.ResponseStatusException;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.input.dto.ShareApplicationCreateRequestDto;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.output.repository.CoownershipListingRepository;
import ru.veshvokrug.coownership.output.repository.OwnershipShareRepository;
import ru.veshvokrug.coownership.output.repository.ShareApplicationRepository;

import java.math.BigDecimal;
import java.time.Clock;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
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

    private ListingService newService(Clock clock) {
        return new ListingService(
                coownershipListingRepository,
                ownershipShareRepository,
                shareApplicationRepository,
                shareApplicationEventPublisher,
                notificationService,
                shareApplicationValidator,
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
        when(coownershipListingRepository.findByCatalogListingId(catalogListingId)).thenReturn(java.util.Optional.empty());
        when(coownershipListingRepository.save(any(CoownershipListing.class))).thenReturn(saved);

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "Дом у моря",
                "Коттедж с участком",
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
    void createListingUsesDefaultFundingDeadlineWhenMissing() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = newService(clock);

        UUID catalogListingId = UUID.randomUUID();
        when(coownershipListingRepository.findByCatalogListingId(catalogListingId)).thenReturn(java.util.Optional.empty());
        when(coownershipListingRepository.save(any(CoownershipListing.class))).thenAnswer(invocation -> {
            CoownershipListing listing = invocation.getArgument(0);
            listing.setId(UUID.randomUUID());
            return listing;
        });

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "Дом у моря",
                "Коттедж с участком",
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
        when(coownershipListingRepository.findByCatalogListingId(catalogListingId)).thenReturn(java.util.Optional.empty());
        when(coownershipListingRepository.save(any(CoownershipListing.class))).thenAnswer(invocation -> {
            CoownershipListing listing = invocation.getArgument(0);
            listing.setId(UUID.randomUUID());
            return listing;
        });

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "Дом у моря",
                "Коттедж с участком",
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
        when(coownershipListingRepository.findByCatalogListingId(catalogListingId)).thenReturn(java.util.Optional.of(existing));

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "Дом у моря",
                "Коттедж с участком",
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
        when(shareApplicationRepository.findByListing_IdAndApplicantId(listingId, applicantId)).thenReturn(Optional.empty());
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
        verify(notificationService, times(1)).createNotification(eq(ownerId), any(ShareApplication.class), eq("SHARE_APPLICATION_CREATED"));
        verify(shareApplicationEventPublisher, times(1)).publish(eq("SHARE_APPLICATION_CREATED"), any(ShareApplication.class));
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
        when(shareApplicationRepository.findByListing_IdAndApplicantId(listingId, applicantId)).thenReturn(Optional.empty());
        when(ownershipShareRepository.countByCoownershipListing_IdAndOwnerIdIsNull(listingId)).thenReturn(2L);

        ShareApplicationCreateRequestDto request = new ShareApplicationCreateRequestDto(applicantId, 3);

        assertThatThrownBy(() -> service.createShareApplication(listingId, request))
                .isInstanceOf(ResponseStatusException.class)
                .hasMessageContaining("Нельзя запросить больше долей");
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

        when(coownershipListingRepository.findWithWriteLockingById(listing.getId())).thenReturn(Optional.of(listing));

        ShareApplication application = new ShareApplication();
        application.setId(applicationId);
        application.setListing(listing);
        application.setApplicantId(applicantId);
        application.setSharesCount(1);
        application.setStatus(ShareApplicationStatus.PENDING);

        when(shareApplicationRepository.findWithLockingById(applicationId)).thenReturn(Optional.of(application));
        when(shareApplicationRepository.save(any(ShareApplication.class))).thenAnswer(invocation -> invocation.getArgument(0));

        ShareApplication result = service.rejectShareApplicationByOwner(applicationId, ownerId);

        assertThat(result.getStatus()).isEqualTo(ShareApplicationStatus.REJECTED);
        verify(notificationService, times(1)).createNotification(eq(applicantId), any(ShareApplication.class), eq("SHARE_APPLICATION_REJECTED"));
        verify(shareApplicationEventPublisher, times(1)).publish(eq("SHARE_APPLICATION_REJECTED"), any(ShareApplication.class));
    }

    @Test
    void getOwnerNotificationsDelegatesToNotificationService() {
        UUID ownerId = UUID.randomUUID();
        ListingService service = newService(Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC));

        service.getOwnerNotifications(ownerId);

        verify(notificationService, times(1)).getNotifications(ownerId);
    }
}
