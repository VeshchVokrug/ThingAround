package ru.veshvokrug.coownership.service;

import org.springframework.data.domain.PageRequest;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.server.ResponseStatusException;
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
    private final Clock clock;

    public ListingService(CoownershipListingRepository coownershipListingRepository,
                          OwnershipShareRepository ownershipShareRepository,
                          ShareApplicationRepository shareApplicationRepository,
                          ShareApplicationEventPublisher shareApplicationEventPublisher,
                          ShareApplicationNotificationService notificationService,
                          ShareApplicationValidator shareApplicationValidator,
                          Clock clock) {
        this.coownershipListingRepository = coownershipListingRepository;
        this.ownershipShareRepository = ownershipShareRepository;
        this.shareApplicationRepository = shareApplicationRepository;
        this.shareApplicationEventPublisher = shareApplicationEventPublisher;
        this.notificationService = notificationService;
        this.shareApplicationValidator = shareApplicationValidator;
        this.clock = clock;
    }

    @Transactional
    public CoownershipListing createListing(CoownershipListingCreateRequestDto createRequestDto) {
        CoownershipListing existingListing = coownershipListingRepository
                .findByCatalogListingId(createRequestDto.catalogListingId())
                .orElse(null);
        if (existingListing != null) {
            return existingListing;
        }

        CoownershipListing listing = new CoownershipListing();
        listing.setName(createRequestDto.name());
        listing.setDescription(createRequestDto.description());
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
        int sharePercentage = Math.max(1, 100 / savedListing.getTotalShares());
        for (int i = 0; i < savedListing.getTotalShares(); i++) {
            OwnershipShare share = new OwnershipShare();
            share.setCoownershipListing(savedListing);
            share.setOwnerId(null);
            share.setPercentage(sharePercentage);
            share.setTemplateDaysMask(0);
            share.setLocked(false);
            shares.add(share);
        }
        ownershipShareRepository.saveAll(shares);

        return savedListing;
    }

    @Transactional
    @SuppressWarnings("unused")
    public ShareApplication approveShareApplication(UUID applicationId) {
        ShareApplication application = shareApplicationRepository.findWithLockingById(applicationId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Заявка не найдена"));

        return approveShareApplication(application);
    }

    private ShareApplication approveShareApplication(ShareApplication application) {
        if (application.getStatus() != ShareApplicationStatus.PENDING) {
            return application;
        }

        CoownershipListing listing = coownershipListingRepository.findWithWriteLockingById(application.getListing().getId())
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Листинг не найден"));

        if (listing.getStatus() != CoownershipStatus.OPEN) {
            throw new ResponseStatusException(HttpStatus.CONFLICT, "Листинг уже закрыт для заявок");
        }

        int requestedShares = application.getSharesCount();
        long freeSharesCount = ownershipShareRepository
                .countByCoownershipListing_IdAndOwnerIdIsNull(listing.getId());
        if (freeSharesCount < requestedShares) {
            throw new ResponseStatusException(
                    HttpStatus.CONFLICT,
                    "Недостаточно свободных долей для одобрения заявки"
            );
        }

        List<OwnershipShare> freeShares = ownershipShareRepository
                .findFreeSharesForUpdate(listing.getId(), PageRequest.of(0, requestedShares));
        if (freeShares.size() < requestedShares) {
            throw new ResponseStatusException(
                    HttpStatus.CONFLICT,
                    "Недостаточно свободных долей для одобрения заявки"
            );
        }

        // Assign shares to applicant
        for (OwnershipShare share : freeShares) {
            share.setOwnerId(application.getApplicantId());
            share.setLocked(true);
        }
        ownershipShareRepository.saveAll(freeShares);

        // Update listing state
        listing.setFilledShares(listing.getFilledShares() + requestedShares);
        if (listing.getFilledShares() >= listing.getTotalShares()) {
            listing.setStatus(CoownershipStatus.FILLED);
        }
        coownershipListingRepository.save(listing);

        // Finalize application
        application.setStatus(ShareApplicationStatus.APPROVED);
        ShareApplication approvedApplication = shareApplicationRepository.save(application);
        notificationService.createNotification(application.getApplicantId(), approvedApplication, "SHARE_APPLICATION_APPROVED");
        shareApplicationEventPublisher.publish("SHARE_APPLICATION_APPROVED", approvedApplication);
        return approvedApplication;
    }

    @Transactional
    public ShareApplication createShareApplication(UUID listingId, ShareApplicationCreateRequestDto requestDto) {
        CoownershipListing listing = coownershipListingRepository.findWithLockingById(listingId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Листинг не найден"));

        if (listing.getStatus() != CoownershipStatus.OPEN) {
            throw new ResponseStatusException(HttpStatus.CONFLICT, "Листинг уже закрыт для заявок");
        }

        if (listing.getOwnerId().equals(requestDto.applicantId())) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Владелец не может подать заявку в свой листинг");
        }

        if (shareApplicationRepository.findByListing_IdAndApplicantId(listingId, requestDto.applicantId()).isPresent()) {
            throw new ResponseStatusException(HttpStatus.CONFLICT, "Заявка от этого пользователя уже существует");
        }

        long availableShares = ownershipShareRepository.countByCoownershipListing_IdAndOwnerIdIsNull(listingId);
        if (requestDto.sharesCount() > availableShares) {
            throw new ResponseStatusException(
                    HttpStatus.BAD_REQUEST,
                    "Нельзя запросить больше долей, чем доступно для покупки"
            );
        }

        ShareApplication shareApplication = new ShareApplication();
        shareApplication.setListing(listing);
        shareApplication.setApplicantId(requestDto.applicantId());
        shareApplication.setSharesCount(requestDto.sharesCount());
        shareApplication.setStatus(ShareApplicationStatus.PENDING);

        ShareApplication savedApplication = shareApplicationRepository.save(shareApplication);
        notificationService.createNotification(listing.getOwnerId(), savedApplication, "SHARE_APPLICATION_CREATED");
        shareApplicationEventPublisher.publish("SHARE_APPLICATION_CREATED", savedApplication);
        return savedApplication;
    }

    @Transactional
    public ShareApplication approveShareApplicationByOwner(UUID applicationId, UUID ownerId) {
        ShareApplication application = shareApplicationRepository.findWithLockingById(applicationId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Заявка не найдена"));

        CoownershipListing listing = coownershipListingRepository.findWithWriteLockingById(application.getListing().getId())
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Листинг не найден"));

        // Delegate validation to specialized component
        shareApplicationValidator.validateOwnerCanApprove(listing, ownerId);

        return approveShareApplication(application);
    }

    @Transactional
    public ShareApplication rejectShareApplicationByOwner(UUID applicationId, UUID ownerId) {
        ShareApplication application = shareApplicationRepository.findWithLockingById(applicationId)
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Заявка не найдена"));

        CoownershipListing listing = coownershipListingRepository.findWithWriteLockingById(application.getListing().getId())
                .orElseThrow(() -> new ResponseStatusException(HttpStatus.NOT_FOUND, "Листинг не найден"));

        // Delegate validation to specialized component
        shareApplicationValidator.validateOwnerCanReject(listing, ownerId);

        if (application.getStatus() == ShareApplicationStatus.APPROVED) {
            throw new ResponseStatusException(HttpStatus.CONFLICT, "Одобренную заявку нельзя отклонить");
        }

        if (application.getStatus() == ShareApplicationStatus.REJECTED) {
            return application;
        }

        application.setStatus(ShareApplicationStatus.REJECTED);
        ShareApplication rejectedApplication = shareApplicationRepository.save(application);
        notificationService.createNotification(application.getApplicantId(), rejectedApplication, "SHARE_APPLICATION_REJECTED");
        shareApplicationEventPublisher.publish("SHARE_APPLICATION_REJECTED", rejectedApplication);
        return rejectedApplication;
    }

    public List<ShareApplicationNotification> getOwnerNotifications(UUID ownerId) {
        return notificationService.getNotifications(ownerId);
    }
}
