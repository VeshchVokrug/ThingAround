package ru.veshvokrug.coownership.input.http;

import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.web.bind.annotation.*;
import ru.veshvokrug.coownership.input.http.dto.PublicListingPageResponseDto;
import ru.veshvokrug.coownership.input.http.dto.PublicListingResponseDto;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.service.ListingService;

import java.util.List;
import java.util.UUID;

/**
 * Публичные REST-эндпоинты для просмотра листингов совладения.
 */
@RestController
@RequestMapping("/api/v1/listings")
public class CoownershipListingController {
    private static final int MAX_PAGE_SIZE = 100;

    private final ListingService listingService;

    public CoownershipListingController(ListingService listingService) {
        this.listingService = listingService;
    }

    @GetMapping
    public PublicListingPageResponseDto getOpenListings(
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "20") int size) {
        if (page < 0) {
            throw new IllegalArgumentException("Параметр page не может быть отрицательным");
        }
        if (size <= 0 || size > MAX_PAGE_SIZE) {
            throw new IllegalArgumentException("Параметр size должен быть в диапазоне 1..100");
        }

        Page<CoownershipListing> listings = listingService.getOpenListings(PageRequest.of(page, size));
        List<PublicListingResponseDto> items = listings.getContent().stream()
                .map(this::toPublicDto)
                .toList();

        return new PublicListingPageResponseDto(
                items,
                listings.getNumber(),
                listings.getSize(),
                listings.getTotalElements(),
                listings.getTotalPages()
        );
    }

    @GetMapping("/{listingId}")
    public PublicListingResponseDto getListingById(@PathVariable String listingId) {
        UUID parsedId;
        try {
            parsedId = UUID.fromString(listingId);
        } catch (Exception ignored) {
            throw new IllegalArgumentException("Неверный формат UUID листинга");
        }

        return toPublicDto(listingService.getListingById(parsedId));
    }

    private PublicListingResponseDto toPublicDto(CoownershipListing listing) {
        return new PublicListingResponseDto(
                listing.getId(),
                listing.getCatalogListingId(),
                listing.getOwnerId(),
                listing.getPrice(),
                listing.getTotalShares(),
                listing.getFilledShares(),
                Math.max(0, listing.getTotalShares() - listing.getFilledShares()),
                listing.getStatus(),
                listing.getFundingDeadline(),
                listing.getCreatedAt()
        );
    }
}

