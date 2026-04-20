package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.HttpStatus;
import org.springframework.web.server.ResponseStatusException;
import ru.veshvokrug.coownership.input.dto.ShareApplicationCreateRequestDto;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThatThrownBy;

/**
 * Unit-тесты для ShareApplicationValidator.
 * <p>
 * Проверяют правила валидации для создания, одобрения и отклонения заявок.
 */
@ExtendWith(MockitoExtension.class)
class ShareApplicationValidatorTest {

    @InjectMocks
    private ShareApplicationValidator validator;

    @Test
    void validateCanCreateApplicationPassesForNormalCase() {
        CoownershipListing listing = buildOpenListing();
        UUID applicantId = UUID.randomUUID();
        ShareApplicationCreateRequestDto requestDto = new ShareApplicationCreateRequestDto(
                applicantId,
                2
        );

        // Should not throw
        validator.validateCanCreateApplication(listing, requestDto);
    }

    @Test
    void validateCanCreateApplicationFailsWhenListingClosed() {
        CoownershipListing listing = buildOpenListing();
        listing.setStatus(CoownershipStatus.FILLED);

        ShareApplicationCreateRequestDto requestDto = new ShareApplicationCreateRequestDto(
                UUID.randomUUID(),
                2
        );

        assertThatThrownBy(() -> validator.validateCanCreateApplication(listing, requestDto))
                .isInstanceOf(ResponseStatusException.class)
                .hasFieldOrPropertyWithValue("status", HttpStatus.CONFLICT);
    }

    @Test
    void validateCanCreateApplicationFailsWhenOwnerApplies() {
        UUID ownerId = UUID.randomUUID();
        CoownershipListing listing = buildOpenListing();
        listing.setOwnerId(ownerId);

        ShareApplicationCreateRequestDto requestDto = new ShareApplicationCreateRequestDto(
                ownerId,  // Same as owner
                2
        );

        assertThatThrownBy(() -> validator.validateCanCreateApplication(listing, requestDto))
                .isInstanceOf(ResponseStatusException.class)
                .hasFieldOrPropertyWithValue("status", HttpStatus.BAD_REQUEST);
    }

    @Test
    void validateOwnerCanApprovePassesForCorrectOwner() {
        UUID ownerId = UUID.randomUUID();
        CoownershipListing listing = buildOpenListing();
        listing.setOwnerId(ownerId);

        // Should not throw
        validator.validateOwnerCanApprove(listing, ownerId);
    }

    @Test
    void validateOwnerCanApproveFailsForWrongOwner() {
        UUID correctOwnerId = UUID.randomUUID();
        UUID wrongOwnerId = UUID.randomUUID();
        CoownershipListing listing = buildOpenListing();
        listing.setOwnerId(correctOwnerId);

        assertThatThrownBy(() -> validator.validateOwnerCanApprove(listing, wrongOwnerId))
                .isInstanceOf(ResponseStatusException.class)
                .hasFieldOrPropertyWithValue("status", HttpStatus.FORBIDDEN);
    }

    @Test
    void validateOwnerCanRejectPassesForCorrectOwner() {
        UUID ownerId = UUID.randomUUID();
        CoownershipListing listing = buildOpenListing();
        listing.setOwnerId(ownerId);

        // Should not throw
        validator.validateOwnerCanReject(listing, ownerId);
    }

    @Test
    void validateOwnerCanRejectFailsForWrongOwner() {
        UUID correctOwnerId = UUID.randomUUID();
        UUID wrongOwnerId = UUID.randomUUID();
        CoownershipListing listing = buildOpenListing();
        listing.setOwnerId(correctOwnerId);

        assertThatThrownBy(() -> validator.validateOwnerCanReject(listing, wrongOwnerId))
                .isInstanceOf(ResponseStatusException.class)
                .hasFieldOrPropertyWithValue("status", HttpStatus.FORBIDDEN);
    }

    // ========== Test Builders ==========

    private CoownershipListing buildOpenListing() {
        CoownershipListing listing = new CoownershipListing();
        listing.setId(UUID.randomUUID());
        listing.setOwnerId(UUID.randomUUID());
        listing.setCatalogListingId(UUID.randomUUID());
        listing.setName("Test Listing");
        listing.setDescription("Test Description");
        listing.setPrice(new BigDecimal("150000.00"));
        listing.setTotalShares(4);
        listing.setFilledShares(0);
        listing.setStatus(CoownershipStatus.OPEN);
        listing.setFundingDeadline(LocalDate.now(ZoneOffset.UTC).plusDays(45));
        return listing;
    }
}
