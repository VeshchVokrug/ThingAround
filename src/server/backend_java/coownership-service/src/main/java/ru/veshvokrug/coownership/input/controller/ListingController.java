package ru.veshvokrug.coownership.input.controller;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateDto;
import ru.veshvokrug.coownership.service.ListingService;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
@RestController
@RequestMapping("/listings")
public class ListingController {
    private final ListingService listingService;

    public ListingController(ListingService listingService) {
        this.listingService = listingService;
    }

    @PostMapping
    public ResponseEntity<CoownershipListingCreateDto> createListing(
            @RequestBody @Validated CoownershipListingCreateDto createRequestDto) {
        return ResponseEntity
                .status(HttpStatus.CREATED)
                .body(listingService.createListing(createRequestDto));
    }
}
