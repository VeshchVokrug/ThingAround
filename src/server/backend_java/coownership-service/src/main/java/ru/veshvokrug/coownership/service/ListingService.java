package ru.veshvokrug.coownership.service;

import org.springframework.data.domain.PageRequest;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.input.dto.ShareApplicationCreateRequestDto;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.OwnershipShare;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.model.entity.ShareApplicationNotification;
import ru.veshvokrug.coownership.output.repository.CoownershipListingRepository;
import ru.veshvokrug.coownership.output.repository.OwnershipShareRepository;
import ru.veshvokrug.coownership.output.repository.ShareApplicationRepository;

import java.time.Clock;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

/**
 * Сервис жизненного цикла листинга совладения и заявок на доли.
 *
 * @author Dmitrii Marchenko 19.04.2026
 */
@Service
public class ListingService {
    private final CoownershipListingRepository coownershipListingRepository;
    private final OwnershipShareRepository ownershipShareRepository;
    private final ShareApplicationRepository shareApplicationRepository;
    private final ShareApplicationEventPublisher shareApplicationEventPublisher;
    private final ShareApplicationNotificationService notificationService;
    private final ShareApplicationValidator shareApplicationValidator;
    private final PeriodLifecycleService periodLifecycleService;
    private final TransactionalLockService transactionalLockService;
    private final Clock clock;

    public ListingService(CoownershipListingRepository coownershipListingRepository,
                          OwnershipShareRepository ownershipShareRepository,
                          ShareApplicationRepository shareApplicationRepository,
                          ShareApplicationEventPublisher shareApplicationEventPublisher,
                          ShareApplicationNotificationService notificationService,
                          ShareApplicationValidator shareApplicationValidator,
                          PeriodLifecycleService periodLifecycleService,
                          TransactionalLockService transactionalLockService,
                          Clock clock) {
        this.coownershipListingRepository = coownershipListingRepository;
        this.ownershipShareRepository = ownershipShareRepository;
        this.shareApplicationRepository = shareApplicationRepository;
        this.shareApplicationEventPublisher = shareApplicationEventPublisher;
        this.notificationService = notificationService;
        this.shareApplicationValidator = shareApplicationValidator;
        this.periodLifecycleService = periodLifecycleService;
        this.transactionalLockService = transactionalLockService;
        this.clock = clock;
    }

    @Transactional
    public CoownershipListing createListing(CoownershipListingCreateRequestDto createRequestDto) {
        validateTotalShares(createRequestDto.totalShares());
        transactionalLockService.lock("coownership-listing:" + createRequestDto.catalogListingId());

        CoownershipListing existingListing = coownershipListingRepository
                .findByCatalogListingId(createRequestDto.catalogListingId())
                .orElse(null);
        if (existingListing != null) {
            return existingListing;
        }

        CoownershipListing listing = new CoownershipListing();
        listing.setCatalogListingId(createRequestDto.catalogListingId());
        listing.setPrice(createRequestDto.price());
        listing.setOwnerId(createRequestDto.ownerId());
        listing.setTotalShares(createRequestDto.totalShares());
        LocalDate deadline = createRequestDto.fundingDeadline() == null
                ? LocalDate.now(clock).plusDays(90)
                : createRequestDto.fundingDeadline();
        listing.setFundingDeadline(deadline);
        CoownershipListing savedListing = coownershipListingRepository.save(listing);

        List<OwnershipShare> shares = new ArrayList<>(savedListing.getTotalShares());
        int basePercentage = 100 / savedListing.getTotalShares();
        int remainder = 100 % savedListing.getTotalShares();
        for (int i = 0; i < savedListing.getTotalShares(); i++) {
            OwnershipShare share = new OwnershipShare();
            share.setCoownershipListing(savedListing);
            share.setOwnerId(null);
            share.setPercentage(basePercentage + (i < remainder ? 1 : 0));
            share.setTemplateDaysMask(0);
            share.setLocked(false);
            shares.add(share);
        }
        ownershipShareRepository.saveAll(shares);

        return savedListing;
    }

    private ShareApplication approveShareApplication(ShareApplication application) {
        if (application.getStatus() != ShareApplicationStatus.PENDING) {
            return application;
        }

        CoownershipListing listing = coownershipListingRepository
                .findWithWriteLockingById(application.getListing().getId())
                .orElseThrow(() -> ServiceException.notFound("Листинг не найден"));

        if (listing.getStatus() != CoownershipStatus.OPEN) {
            throw ServiceException.conflict("Листинг уже закрыт для заявок");
        }

        int requestedShares = application.getSharesCount();
        long freeSharesCount = ownershipShareRepository
                .countByCoownershipListing_IdAndOwnerIdIsNull(listing.getId());
        if (freeSharesCount < requestedShares) {
            throw ServiceException.conflict(
                    "Недостаточно свободных долей для одобрения заявки"
            );
        }

        List<OwnershipShare> freeShares = ownershipShareRepository
                .findFreeSharesForUpdate(listing.getId(), PageRequest.of(0, requestedShares));
        if (freeShares.size() < requestedShares) {
            throw ServiceException.conflict(
                    "Недостаточно свободных долей для одобрения заявки"
            );
        }

        for (OwnershipShare share : freeShares) {
            share.setOwnerId(application.getApplicantId());
            share.setLocked(false);
        }
        ownershipShareRepository.saveAll(freeShares);

        listing.setFilledShares(listing.getFilledShares() + requestedShares);
        if (listing.getFilledShares() >= listing.getTotalShares()) {
            listing.setStatus(CoownershipStatus.FILLED);
            lockAllListingShares(listing.getId());
            periodLifecycleService.triggerFilledOut(listing);
        }
        coownershipListingRepository.save(listing);

        application.setStatus(ShareApplicationStatus.APPROVED);
        ShareApplication approvedApplication = shareApplicationRepository.save(application);
        notificationService.createNotification(
                application.getApplicantId(),
                approvedApplication,
                "SHARE_APPLICATION_APPROVED");
        shareApplicationEventPublisher.publish(
                "SHARE_APPLICATION_APPROVED",
                approvedApplication);
        return approvedApplication;
    }

    @Transactional
    public ShareApplication createShareApplication(
            UUID listingId,
            ShareApplicationCreateRequestDto requestDto) {
        validateSharesCount(requestDto.sharesCount());

        CoownershipListing listing = coownershipListingRepository.findWithWriteLockingById(listingId)
                .orElseThrow(() -> ServiceException.notFound("Листинг не найден"));

        if (listing.getStatus() != CoownershipStatus.OPEN) {
            throw ServiceException.conflict("Листинг уже закрыт для заявок");
        }

        if (listing.getOwnerId().equals(requestDto.applicantId())) {
            throw ServiceException.badRequest("Владелец не может подать заявку в свой листинг");
        }

        if (shareApplicationRepository.findByListing_IdAndApplicantId(
                listingId,
                requestDto
                        .applicantId())
                .isPresent()) {
            throw ServiceException.conflict("Заявка от этого пользователя уже существует");
        }

        long availableShares = ownershipShareRepository
                .countByCoownershipListing_IdAndOwnerIdIsNull(listingId);
        if (requestDto.sharesCount() > availableShares) {
            throw ServiceException.badRequest(
                    "Нельзя запросить больше долей, чем доступно для покупки"
            );
        }

        ShareApplication shareApplication = new ShareApplication();
        shareApplication.setListing(listing);
        shareApplication.setApplicantId(requestDto.applicantId());
        shareApplication.setSharesCount(requestDto.sharesCount());
        shareApplication.setStatus(ShareApplicationStatus.PENDING);

        ShareApplication savedApplication = shareApplicationRepository.save(shareApplication);
        notificationService.createNotification(
                listing.getOwnerId(),
                savedApplication,
                "SHARE_APPLICATION_CREATED");
        shareApplicationEventPublisher
                .publish("SHARE_APPLICATION_CREATED", savedApplication);
        return savedApplication;
    }

    @Transactional
    public ShareApplication approveShareApplicationByOwner(UUID applicationId, UUID ownerId) {
        ShareApplication application = shareApplicationRepository.findWithLockingById(applicationId)
                .orElseThrow(() -> ServiceException.notFound("Заявка не найдена"));

        CoownershipListing listing = coownershipListingRepository
                .findWithWriteLockingById(application.getListing().getId())
                .orElseThrow(() -> ServiceException.notFound("Листинг не найден"));

        shareApplicationValidator.validateOwnerCanApprove(listing, ownerId);

        return approveShareApplication(application);
    }

    @Transactional
    public ShareApplication rejectShareApplicationByOwner(UUID applicationId, UUID ownerId) {
        ShareApplication application = shareApplicationRepository.findWithLockingById(applicationId)
                .orElseThrow(() -> ServiceException.notFound("Заявка не найдена"));

        CoownershipListing listing = coownershipListingRepository
                .findWithWriteLockingById(application.getListing().getId())
                .orElseThrow(() -> ServiceException.notFound("Листинг не найден"));

        shareApplicationValidator.validateOwnerCanReject(listing, ownerId);

        if (application.getStatus() == ShareApplicationStatus.APPROVED) {
            throw ServiceException.conflict("Одобренную заявку нельзя отклонить");
        }

        if (application.getStatus() == ShareApplicationStatus.REJECTED) {
            return application;
        }

        application.setStatus(ShareApplicationStatus.REJECTED);
        ShareApplication rejectedApplication = shareApplicationRepository.save(application);
        notificationService.createNotification(
                application.getApplicantId(),
                rejectedApplication,
                "SHARE_APPLICATION_REJECTED");
        shareApplicationEventPublisher.publish(
                "SHARE_APPLICATION_REJECTED",
                rejectedApplication);
        return rejectedApplication;
    }

    public List<ShareApplicationNotification> getOwnerNotifications(UUID ownerId) {
        return notificationService.getNotifications(ownerId);
    }

    private void lockAllListingShares(UUID listingId) {
        List<OwnershipShare> allShares = ownershipShareRepository.findByCoownershipListing_Id(listingId);
        for (OwnershipShare share : allShares) {
            share.setLocked(true);
        }
        ownershipShareRepository.saveAll(allShares);
    }

    private void validateTotalShares(int totalShares) {
        if (totalShares < 2 || totalShares > 10) {
            throw ServiceException.badRequest("Количество долей должно быть от 2 до 10");
        }
    }

    private void validateSharesCount(int sharesCount) {
        if (sharesCount <= 0) {
            throw ServiceException.badRequest("Количество долей должно быть больше 0");
        }
    }
}
