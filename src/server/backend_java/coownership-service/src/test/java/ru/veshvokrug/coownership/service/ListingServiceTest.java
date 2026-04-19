package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.output.repository.CoownershipListingRepository;
import ru.veshvokrug.coownership.output.repository.OwnershipShareRepository;
import ru.veshvokrug.coownership.output.repository.ShareApplicationRepository;

import java.math.BigDecimal;
import java.time.Clock;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyList;
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

    @Test
    void createListingUsesProvidedFundingDeadline() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = new ListingService(
                coownershipListingRepository,
                ownershipShareRepository,
                shareApplicationRepository,
                clock
        );

        UUID catalogListingId = UUID.randomUUID();
        CoownershipListing saved = new CoownershipListing();
        saved.setId(UUID.randomUUID());
        saved.setCatalogListingId(catalogListingId);
        saved.setTotalShares(10);
        saved.setFundingDeadline(LocalDate.of(2026, 7, 1));
        when(coownershipListingRepository.findByCatalogListingId(catalogListingId)).thenReturn(java.util.Optional.empty());
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
    void createListingUsesDefaultFundingDeadlineWhenMissing() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = new ListingService(
                coownershipListingRepository,
                ownershipShareRepository,
                shareApplicationRepository,
                clock
        );

        UUID catalogListingId = UUID.randomUUID();
        when(coownershipListingRepository.findByCatalogListingId(catalogListingId)).thenReturn(java.util.Optional.empty());
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
        ListingService service = new ListingService(
                coownershipListingRepository,
                ownershipShareRepository,
                shareApplicationRepository,
                clock
        );

        UUID ownerId = UUID.randomUUID();
        UUID catalogListingId = UUID.randomUUID();
        when(coownershipListingRepository.findByCatalogListingId(catalogListingId)).thenReturn(java.util.Optional.empty());
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
        ListingService service = new ListingService(
                coownershipListingRepository,
                ownershipShareRepository,
                shareApplicationRepository,
                clock
        );

        UUID catalogListingId = UUID.randomUUID();
        CoownershipListing existing = new CoownershipListing();
        existing.setId(UUID.randomUUID());
        existing.setCatalogListingId(catalogListingId);
        when(coownershipListingRepository.findByCatalogListingId(catalogListingId)).thenReturn(java.util.Optional.of(existing));

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
}
