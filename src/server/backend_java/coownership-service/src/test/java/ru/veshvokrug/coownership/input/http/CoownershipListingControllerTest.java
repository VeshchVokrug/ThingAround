package ru.veshvokrug.coownership.input.http;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.webmvc.test.autoconfigure.WebMvcTest;
import org.springframework.data.domain.PageImpl;
import org.springframework.data.domain.PageRequest;
import org.springframework.http.MediaType;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.service.ListingService;
import ru.veshvokrug.coownership.service.ServiceException;

import java.math.BigDecimal;
import java.time.Instant;
import java.time.LocalDate;
import java.util.List;
import java.util.UUID;

import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@WebMvcTest(CoownershipListingController.class)
class CoownershipListingControllerTest {

    @Autowired
    private MockMvc mockMvc;

    @MockitoBean
    private ListingService listingService;

    @Test
    void getOpenListingsReturnsPaginatedItems() throws Exception {
        UUID listingId = UUID.fromString("11111111-1111-1111-1111-111111111111");
        UUID catalogListingId = UUID.fromString("22222222-2222-2222-2222-222222222222");
        UUID ownerId = UUID.fromString("33333333-3333-3333-3333-333333333333");

        CoownershipListing listing = listing(
                listingId,
                catalogListingId,
                ownerId,
                10,
                4,
                CoownershipStatus.OPEN
        );

        when(listingService.getOpenListings(PageRequest.of(0, 20)))
                .thenReturn(new PageImpl<>(List.of(listing), PageRequest.of(0, 20), 1));

        mockMvc.perform(get("/api/v1/listings")
                        .param("page", "0")
                        .param("size", "20")
                        .accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.items.length()").value(1))
                .andExpect(jsonPath("$.items[0].id").value(listingId.toString()))
                .andExpect(jsonPath("$.items[0].catalogListingId").value(catalogListingId.toString()))
                .andExpect(jsonPath("$.items[0].ownerId").value(ownerId.toString()))
                .andExpect(jsonPath("$.items[0].price").value(150000.00))
                .andExpect(jsonPath("$.items[0].totalShares").value(10))
                .andExpect(jsonPath("$.items[0].filledShares").value(4))
                .andExpect(jsonPath("$.items[0].availableShares").value(6))
                .andExpect(jsonPath("$.items[0].status").value("OPEN"))
                .andExpect(jsonPath("$.items[0].fundingDeadline").value("2026-08-01"))
                .andExpect(jsonPath("$.page").value(0))
                .andExpect(jsonPath("$.size").value(20))
                .andExpect(jsonPath("$.totalElements").value(1))
                .andExpect(jsonPath("$.totalPages").value(1));

        verify(listingService).getOpenListings(PageRequest.of(0, 20));
    }

    @Test
    void getListingByIdReturnsListing() throws Exception {
        UUID listingId = UUID.fromString("44444444-4444-4444-4444-444444444444");
        UUID catalogListingId = UUID.fromString("55555555-5555-5555-5555-555555555555");
        UUID ownerId = UUID.fromString("66666666-6666-6666-6666-666666666666");

        when(listingService.getListingById(listingId))
                .thenReturn(listing(listingId, catalogListingId, ownerId, 8, 8, CoownershipStatus.FILLED));

        mockMvc.perform(get("/api/v1/listings/{listingId}", listingId)
                        .accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(listingId.toString()))
                .andExpect(jsonPath("$.status").value("FILLED"))
                .andExpect(jsonPath("$.availableShares").value(0));
    }

    @Test
    void getListingByIdReturnsNotFoundWhenMissing() throws Exception {
        UUID listingId = UUID.fromString("77777777-7777-7777-7777-777777777777");
        when(listingService.getListingById(listingId))
                .thenThrow(ServiceException.notFound("Листинг не найден"));

        mockMvc.perform(get("/api/v1/listings/{listingId}", listingId)
                        .accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isNotFound())
                .andExpect(jsonPath("$.message").value("Листинг не найден"));
    }

    @Test
    void getListingByIdReturnsBadRequestForInvalidUuid() throws Exception {
        mockMvc.perform(get("/api/v1/listings/{listingId}", "not-a-uuid")
                        .accept(MediaType.APPLICATION_JSON))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.message").value("Неверный формат UUID листинга"));
    }

    private CoownershipListing listing(
            UUID listingId,
            UUID catalogListingId,
            UUID ownerId,
            int totalShares,
            int filledShares,
            CoownershipStatus status) {
        CoownershipListing listing = new CoownershipListing();
        listing.setId(listingId);
        listing.setCatalogListingId(catalogListingId);
        listing.setOwnerId(ownerId);
        listing.setPrice(new BigDecimal("150000.00"));
        listing.setTotalShares(totalShares);
        listing.setFilledShares(filledShares);
        listing.setStatus(status);
        listing.setFundingDeadline(LocalDate.of(2026, 8, 1));
        listing.setCreatedAt(Instant.parse("2026-05-01T10:15:30Z"));
        return listing;
    }
}

