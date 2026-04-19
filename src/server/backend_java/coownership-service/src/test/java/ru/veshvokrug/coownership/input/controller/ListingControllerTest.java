package ru.veshvokrug.coownership.input.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.MediaType;
import org.springframework.http.converter.json.JacksonJsonHttpMessageConverter;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import org.springframework.validation.beanvalidation.LocalValidatorFactoryBean;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateResponseDto;
import ru.veshvokrug.coownership.input.mapper.CoownershipListingMapper;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.service.ListingService;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.UUID;

import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

/**
 * Тесты HTTP-слоя создания листинга.
 * <p>
 * Проверяются сценарии успешного запроса и основные ошибки валидации входного DTO.
 */
@ExtendWith(MockitoExtension.class)
class ListingControllerTest {

    private final ObjectMapper objectMapper = new ObjectMapper().findAndRegisterModules();

    @Mock
    private ListingService listingService;

    @Mock
    private CoownershipListingMapper coownershipListingMapper;

    private MockMvc mockMvc;

    @BeforeEach
    void setUp() {
        LocalValidatorFactoryBean validator = new LocalValidatorFactoryBean();
        validator.afterPropertiesSet();

        mockMvc = MockMvcBuilders.standaloneSetup(new ListingController(listingService, coownershipListingMapper))
                .setControllerAdvice(new GlobalExceptionHandler())
                .setValidator(validator)
                .setMessageConverters(new JacksonJsonHttpMessageConverter())
                .build();
    }

    @Test
    void createListingReturnsCreatedResponse() throws Exception {
        CoownershipListingCreateRequestDto request = validRequest(LocalDate.now().plusDays(45));
        stubCreatedResponse(request, request.fundingDeadline());

        mockMvc.perform(post("/listings")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.id").exists())
                .andExpect(jsonPath("$.fundingDeadline").value(request.fundingDeadline().toString()))
                .andExpect(jsonPath("$.status").value("OPEN"));
    }

    @Test
    void createListingReturnsDefaultFundingDeadlineWhenRequestMissingValue() throws Exception {
        CoownershipListingCreateRequestDto request = validRequest(null);
        LocalDate expectedDeadline = LocalDate.now().plusDays(90);
        stubCreatedResponse(request, expectedDeadline);

        mockMvc.perform(post("/listings")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.fundingDeadline").value(expectedDeadline.toString()))
                .andExpect(jsonPath("$.status").value("OPEN"));
    }

    @Test
    void createListingAcceptsFundingDeadlineAtLowerBoundary() throws Exception {
        CoownershipListingCreateRequestDto request = validRequest(LocalDate.now().plusDays(30));
        stubCreatedResponse(request, request.fundingDeadline());

        mockMvc.perform(post("/listings")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isCreated());
    }

    @Test
    void createListingAcceptsFundingDeadlineAtUpperBoundary() throws Exception {
        CoownershipListingCreateRequestDto request = validRequest(LocalDate.now().plusYears(1));
        stubCreatedResponse(request, request.fundingDeadline());

        mockMvc.perform(post("/listings")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isCreated());
    }

    @Test
    void createListingRejectsHtmlLikeName() throws Exception {
        UUID ownerId = UUID.randomUUID();
        LocalDate deadline = LocalDate.now().plusDays(45);

        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "<script>alert(1)</script>",
                "Описание объекта",
                new BigDecimal("150000.00"),
                ownerId,
                10,
                deadline
        );

        mockMvc.perform(post("/listings")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.status").value(400))
                .andExpect(jsonPath("$.errorMessage").exists());
    }

    @Test
    void createListingRejectsFundingDeadlineOutsideWindow() throws Exception {
        CoownershipListingCreateRequestDto request = validRequest(LocalDate.now().plusDays(10));

        mockMvc.perform(post("/listings")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.status").value(400))
                .andExpect(jsonPath("$.errorMessage").exists());
    }

    @Test
    void createListingRejectsFundingDeadlineAfterUpperBoundary() throws Exception {
        CoownershipListingCreateRequestDto request = validRequest(LocalDate.now().plusYears(1).plusDays(1));

        mockMvc.perform(post("/listings")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.status").value(400))
                .andExpect(jsonPath("$.errorMessage").exists());
    }

    @Test
    void createListingRejectsNegativeSharePrice() throws Exception {
        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "Апартаменты у моря",
                "Описание объекта",
                new BigDecimal("-1.00"),
                UUID.randomUUID(),
                10,
                LocalDate.now().plusDays(45)
        );

        mockMvc.perform(post("/listings")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.status").value(400));
    }

    @Test
    void createListingRejectsTotalSharesOutOfRange() throws Exception {
        int invalidTotalShares = 1;
        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "Апартаменты у моря",
                "Описание объекта",
                new BigDecimal("150000.00"),
                UUID.randomUUID(),
                invalidTotalShares,
                LocalDate.now().plusDays(45)
        );

        mockMvc.perform(post("/listings")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isBadRequest())
                .andExpect(jsonPath("$.status").value(400))
                .andExpect(jsonPath("$.errorMessage").exists());
    }

    private CoownershipListingCreateRequestDto validRequest(LocalDate deadline) {
        return new CoownershipListingCreateRequestDto(
                "Апартаменты у моря",
                "Описание объекта",
                new BigDecimal("150000.00"),
                UUID.randomUUID(),
                10,
                deadline
        );
    }

    private void stubCreatedResponse(CoownershipListingCreateRequestDto request, LocalDate responseDeadline) {
        CoownershipListing listing = new CoownershipListing();
        listing.setId(UUID.randomUUID());
        listing.setName(request.name());
        listing.setDescription(request.description());
        listing.setPrice(request.sharePrice());
        listing.setOwnerId(request.ownerId());
        listing.setTotalShares(request.totalShares());
        listing.setFundingDeadline(responseDeadline);
        listing.setStatus(CoownershipStatus.OPEN);

        when(listingService.createListing(any(CoownershipListingCreateRequestDto.class))).thenReturn(listing);
        when(coownershipListingMapper.toCreateResponseDto(listing)).thenReturn(
                new CoownershipListingCreateResponseDto(listing.getId(), listing.getFundingDeadline(), listing.getStatus())
        );
    }
}
