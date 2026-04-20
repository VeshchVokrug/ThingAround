package ru.veshvokrug.coownership.input.controller;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import ru.veshvokrug.coownership.input.dto.*;
import ru.veshvokrug.coownership.input.mapper.CoownershipListingMapper;
import ru.veshvokrug.coownership.input.mapper.ShareApplicationMapper;
import ru.veshvokrug.coownership.input.mapper.ShareApplicationNotificationMapper;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.service.ListingService;

import java.util.List;
import java.util.UUID;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
@RestController
@RequestMapping("/listings")
@SuppressWarnings("XSS")
public class ListingController {
    private final ListingService listingService;
    private final CoownershipListingMapper coownershipListingMapper;
    private final ShareApplicationMapper shareApplicationMapper;
    private final ShareApplicationNotificationMapper shareApplicationNotificationMapper;

    public ListingController(
            ListingService listingService,
            CoownershipListingMapper coownershipListingMapper,
            ShareApplicationMapper shareApplicationMapper,
            ShareApplicationNotificationMapper shareApplicationNotificationMapper) {
        this.listingService = listingService;
        this.coownershipListingMapper = coownershipListingMapper;
        this.shareApplicationMapper = shareApplicationMapper;
        this.shareApplicationNotificationMapper = shareApplicationNotificationMapper;
    }

    @PostMapping
    @Operation(
            summary = "Создать листинг совладения",
            description = "Если fundingDeadline не передан, сервис автоматически " +
                    "выставляет дедлайн на +90 дней от текущей даты. " +
                    "Если листинг с тем же catalogListingId уже существует, " +
                    "сервис вернет его без дублирования."
    )
    @ApiResponses({
            @ApiResponse(
                    responseCode = "201",
                    description = "Листинг создан",
                    content = @Content(
                            schema = @Schema(implementation = CoownershipListingCreateResponseDto.class))),
            @ApiResponse(
                    responseCode = "400",
                    description = "Ошибка валидации входных данных",
                    content = @Content(
                            schema = @Schema(implementation = ErrorResponseDto.class))),
            @ApiResponse(
                    responseCode = "500",
                    description = "Внутренняя ошибка сервиса",
                    content = @Content(
                            schema = @Schema(implementation = ErrorResponseDto.class)))
    })
    @SuppressWarnings("XSS")
    public ResponseEntity<CoownershipListingCreateResponseDto> createListing(
            @RequestBody @Valid CoownershipListingCreateRequestDto createRequestDto) {

        CoownershipListing serviceResponse = listingService.createListing(createRequestDto);
        return ResponseEntity
                .status(HttpStatus.CREATED)
                .body(coownershipListingMapper.toCreateResponseDto(serviceResponse));
    }

    @PostMapping("/{listingId}/share-applications")
    @Operation(summary = "Создать заявку на долю")
    public ResponseEntity<ShareApplicationResponseDto> createShareApplication(
            @PathVariable UUID listingId,
            @RequestBody @Valid ShareApplicationCreateRequestDto requestDto
    ) {
        ShareApplication application = listingService.createShareApplication(listingId, requestDto);
        return ResponseEntity
                .status(HttpStatus.CREATED)
                .body(shareApplicationMapper.toResponseDto(application));
    }

    @PostMapping("/share-applications/{applicationId}/approve")
    @Operation(summary = "Подтвердить заявку владельцем")
    public ResponseEntity<ShareApplicationResponseDto> approveShareApplication(
            @PathVariable UUID applicationId,
            @RequestParam UUID ownerId
    ) {
        ShareApplication application = listingService.approveShareApplicationByOwner(applicationId, ownerId);
        return ResponseEntity.ok(shareApplicationMapper.toResponseDto(application));
    }

    @PostMapping("/share-applications/{applicationId}/reject")
    @Operation(summary = "Отклонить заявку владельцем")
    public ResponseEntity<ShareApplicationResponseDto> rejectShareApplication(
            @PathVariable UUID applicationId,
            @RequestParam UUID ownerId
    ) {
        ShareApplication application = listingService.rejectShareApplicationByOwner(applicationId, ownerId);
        return ResponseEntity.ok(shareApplicationMapper.toResponseDto(application));
    }

    @GetMapping({"/owners/{ownerId}/notifications", "/owners/{ownerId}/notifications/share-applications"})
    @Operation(summary = "Получить polling-уведомления владельца")
    public ResponseEntity<List<ShareApplicationNotificationDto>> getOwnerShareApplicationNotifications(
            @PathVariable UUID ownerId
    ) {
        List<ru.veshvokrug.coownership.model.entity.ShareApplicationNotification> notifications = listingService.getOwnerNotifications(ownerId);
        List<ShareApplicationNotificationDto> response = notifications
                .stream()
                .map(shareApplicationNotificationMapper::toDto)
                .toList();
        return ResponseEntity.ok(response);
    }
}
