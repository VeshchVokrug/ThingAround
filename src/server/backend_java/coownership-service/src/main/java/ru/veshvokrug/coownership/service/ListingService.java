package ru.veshvokrug.coownership.service;

import org.springframework.stereotype.Service;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.output.repository.CoownershipListingRepository;

import java.time.Clock;
import java.time.LocalDate;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
@Service
public class ListingService {
    private final CoownershipListingRepository coownershipListingRepository;
    // Внедренный источник времени делает логику дедлайна детерминированной и удобной для тестов.
    private final Clock clock;

    public ListingService(CoownershipListingRepository coownershipListingRepository, Clock clock) {
        this.coownershipListingRepository = coownershipListingRepository;
        this.clock = clock;
    }

    public CoownershipListing createListing(CoownershipListingCreateRequestDto createRequestDto) {
        CoownershipListing listing = new CoownershipListing();
        listing.setName(createRequestDto.name());
        listing.setDescription(createRequestDto.description());
        listing.setPrice(createRequestDto.sharePrice());
        listing.setOwnerId(createRequestDto.ownerId());
        listing.setTotalShares(createRequestDto.totalShares());
        LocalDate deadline = createRequestDto.fundingDeadline() == null
                ? LocalDate.now(clock).plusDays(90)
                : createRequestDto.fundingDeadline();
        listing.setFundingDeadline(deadline);

        return coownershipListingRepository.save(listing);
    }
}
