package ru.veshvokrug.coownership.service;

import org.springframework.data.domain.PageRequest;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.OwnershipShare;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
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
    // Внедренный источник времени делает логику дедлайна детерминированной и удобной для тестов.
    private final Clock clock;

    public ListingService(CoownershipListingRepository coownershipListingRepository,
                          OwnershipShareRepository ownershipShareRepository,
                          ShareApplicationRepository shareApplicationRepository,
                          Clock clock) {
        this.coownershipListingRepository = coownershipListingRepository;
        this.ownershipShareRepository = ownershipShareRepository;
        this.shareApplicationRepository = shareApplicationRepository;
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
    public ShareApplication approveShareApplication(UUID applicationId) {
        ShareApplication application = shareApplicationRepository.findWithLockingById(applicationId)
                .orElseThrow(() -> new IllegalArgumentException("Заявка не найдена"));

        if (application.getStatus() != ShareApplicationStatus.PENDING) {
            return application;
        }

        CoownershipListing listing = application.getListing();
        int requestedShares = application.getSharesCount();
        long freeSharesCount = ownershipShareRepository
                .countByCoownershipListing_IdAndOwnerIdIsNull(listing.getId());
        if (freeSharesCount < requestedShares) {
            throw new IllegalStateException("Недостаточно свободных долей для одобрения заявки");
        }

        List<OwnershipShare> freeShares = ownershipShareRepository
                .findFreeSharesForUpdate(listing.getId(), PageRequest.of(0, requestedShares));
        if (freeShares.size() < requestedShares) {
            throw new IllegalStateException("Недостаточно свободных долей для одобрения заявки");
        }

        for (OwnershipShare share : freeShares) {
            share.setOwnerId(application.getApplicantId());
            share.setLocked(true);
        }
        ownershipShareRepository.saveAll(freeShares);

        listing.setFilledShares(listing.getFilledShares() + requestedShares);
        if (listing.getFilledShares() >= listing.getTotalShares()) {
            listing.setStatus(CoownershipStatus.FILLED);
        }
        coownershipListingRepository.save(listing);

        application.setStatus(ShareApplicationStatus.APPROVED);
        return shareApplicationRepository.save(application);
    }
}
