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
import ru.veshvokrug.coownership.input.dto.*;
import ru.veshvokrug.coownership.input.mapper.CoownershipListingMapper;
import ru.veshvokrug.coownership.input.mapper.ShareApplicationMapper;
import ru.veshvokrug.coownership.input.mapper.ShareApplicationNotificationMapper;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.model.entity.ShareApplicationNotification;
import ru.veshvokrug.coownership.service.ListingService;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.List;
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

    @Mock
    private ShareApplicationMapper shareApplicationMapper;

    @Mock
    private ShareApplicationNotificationMapper shareApplicationNotificationMapper;

    private MockMvc mockMvc;

    @BeforeEach
    void setUp() {
        LocalValidatorFactoryBean validator = new LocalValidatorFactoryBean();
        validator.afterPropertiesSet();

        mockMvc = MockMvcBuilders.standaloneSetup(
                        new ListingController(
                                listingService,
                                coownershipListingMapper,
                                shareApplicationMapper,
                                shareApplicationNotificationMapper
                        )
                )
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
    void createListingRejectsMissingCatalogListingId() throws Exception {
        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "Дом у моря",
                "Коттедж с участком",
                null,
                new BigDecimal("150000.00"),
                UUID.randomUUID(),
                10,
                LocalDate.now().plusDays(45)
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
                "Дом у моря",
                "Коттедж с участком",
                UUID.randomUUID(),
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
        int invalidTotalShares = invalidTotalSharesBelowMinimum();
        CoownershipListingCreateRequestDto request = new CoownershipListingCreateRequestDto(
                "Дом у моря",
                "Коттедж с участком",
                UUID.randomUUID(),
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

    private int invalidTotalSharesBelowMinimum() {
        return Integer.parseInt("1");
    }

    @Test
    void createShareApplicationReturnsCreatedResponse() throws Exception {
        UUID listingId = UUID.randomUUID();
        UUID applicantId = UUID.randomUUID();
        ShareApplicationCreateRequestDto request = new ShareApplicationCreateRequestDto(applicantId, 2);

        ShareApplication application = new ShareApplication();
        application.setId(UUID.randomUUID());
        CoownershipListing listing = new CoownershipListing();
        listing.setId(listingId);
        application.setListing(listing);
        application.setApplicantId(applicantId);
        application.setSharesCount(2);
        application.setStatus(ShareApplicationStatus.PENDING);

        ShareApplicationResponseDto responseDto = new ShareApplicationResponseDto(
                application.getId(),
                listingId,
                applicantId,
                2,
                ShareApplicationStatus.PENDING
        );

        when(listingService.createShareApplication(any(UUID.class), any(ShareApplicationCreateRequestDto.class)))
                .thenReturn(application);
        when(shareApplicationMapper.toResponseDto(application)).thenReturn(responseDto);

        mockMvc.perform(post("/listings/{listingId}/share-applications", listingId)
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(request)))
                .andExpect(status().isCreated())
                .andExpect(jsonPath("$.id").value(application.getId().toString()))
                .andExpect(jsonPath("$.listingId").value(listingId.toString()))
                .andExpect(jsonPath("$.applicantId").value(applicantId.toString()))
                .andExpect(jsonPath("$.sharesCount").value(2))
                .andExpect(jsonPath("$.status").value("PENDING"));
    }

    @Test
    void getOwnerNotificationsReturnsPollingReadModel() throws Exception {
        UUID ownerId = UUID.randomUUID();
        ShareApplicationNotification notification = new ShareApplicationNotification();
        notification.setId(UUID.randomUUID());
        notification.setRecipientId(ownerId);
        notification.setApplicationId(UUID.randomUUID());
        notification.setListingId(UUID.randomUUID());
        notification.setOwnerId(ownerId);
        notification.setApplicantId(UUID.randomUUID());
        notification.setSharesCount(2);
        notification.setEventType("SHARE_APPLICATION_CREATED");
        notification.setApplicationStatus(ShareApplicationStatus.PENDING);

        ShareApplicationNotificationDto responseDto = new ShareApplicationNotificationDto(
                notification.getId(),
                ownerId,
                notification.getApplicationId(),
                notification.getListingId(),
                ownerId,
                notification.getApplicantId(),
                2,
                ShareApplicationStatus.PENDING,
                "SHARE_APPLICATION_CREATED",
                notification.getCreatedAt(),
                notification.getExpiresAt()
        );

        List<ShareApplicationNotification> notifications = java.util.List.of(notification);
        when(listingService.getOwnerNotifications(ownerId)).thenReturn(notifications);
        when(shareApplicationNotificationMapper.toDto(notification)).thenReturn(responseDto);


        mockMvc.perform(org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get("/listings/owners/{ownerId}/notifications", ownerId))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$[0].recipientId").value(ownerId.toString()))
                .andExpect(jsonPath("$[0].eventType").value("SHARE_APPLICATION_CREATED"))
                .andExpect(jsonPath("$[0].sharesCount").value(2));
    }

    private CoownershipListingCreateRequestDto validRequest(LocalDate deadline) {
        return new CoownershipListingCreateRequestDto(
                "Дом у моря",
                "Коттедж с участком",
                UUID.randomUUID(),
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
        listing.setCatalogListingId(request.catalogListingId());
        listing.setPrice(request.price());
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
