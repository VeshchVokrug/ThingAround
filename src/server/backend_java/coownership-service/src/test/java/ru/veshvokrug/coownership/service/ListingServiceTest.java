package ru.veshvokrug.coownership.service;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.ArgumentCaptor;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.output.repository.CoownershipListingRepository;

import java.math.BigDecimal;
import java.time.Clock;
import java.time.Instant;
import java.time.LocalDate;
import java.time.ZoneOffset;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.when;

/**
 * Unit-тесты бизнес-логики создания листинга в сервисе.
 * <p>
 * Проверяются выбор дедлайна и корректный перенос полей из запроса в сущность.
 */
@ExtendWith(MockitoExtension.class)
class ListingServiceTest {

    @Mock
    private CoownershipListingRepository coownershipListingRepository;

    @Test
    void createListingUsesProvidedFundingDeadline() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = new ListingService(coownershipListingRepository, clock);

        CoownershipListing saved = new CoownershipListing();
        saved.setFundingDeadline(LocalDate.of(2026, 7, 1));
        when(coownershipListingRepository.save(any(CoownershipListing.class))).thenReturn(saved);

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "Апартаменты у моря",
                "Описание объекта",
                new BigDecimal("150000.00"),
                UUID.randomUUID(),
                10,
                LocalDate.of(2026, 7, 1)
        );

        service.createListing(request);

        ArgumentCaptor<CoownershipListing> captor = ArgumentCaptor.forClass(CoownershipListing.class);
        org.mockito.Mockito.verify(coownershipListingRepository).save(captor.capture());
        assertThat(captor.getValue().getFundingDeadline()).isEqualTo(LocalDate.of(2026, 7, 1));
    }

    @Test
    void createListingUsesDefaultFundingDeadlineWhenMissing() {
        Clock clock = Clock.fixed(Instant.parse("2026-04-19T00:00:00Z"), ZoneOffset.UTC);
        ListingService service = new ListingService(coownershipListingRepository, clock);

        when(coownershipListingRepository.save(any(CoownershipListing.class))).thenAnswer(invocation -> invocation.getArgument(0));

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "Апартаменты у моря",
                "Описание объекта",
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
        ListingService service = new ListingService(coownershipListingRepository, clock);

        when(coownershipListingRepository.save(any(CoownershipListing.class))).thenAnswer(invocation -> invocation.getArgument(0));

        UUID ownerId = UUID.randomUUID();
        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "Апартаменты у моря",
                "Описание объекта",
                new BigDecimal("150000.00"),
                ownerId,
                8,
                null
        );

        CoownershipListing result = service.createListing(request);

        assertThat(result.getName()).isEqualTo(request.name());
        assertThat(result.getDescription()).isEqualTo(request.description());
        assertThat(result.getPrice()).isEqualByComparingTo(request.sharePrice());
        assertThat(result.getOwnerId()).isEqualTo(ownerId);
        assertThat(result.getTotalShares()).isEqualTo(8);
        assertThat(result.getFundingDeadline()).isEqualTo(LocalDate.of(2026, 7, 18));
    }
}
