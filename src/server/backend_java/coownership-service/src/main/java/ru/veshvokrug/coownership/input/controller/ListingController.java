package ru.veshvokrug.coownership.input.controller;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import jakarta.validation.Valid;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateResponseDto;
import ru.veshvokrug.coownership.input.dto.ErrorResponseDto;
import ru.veshvokrug.coownership.input.mapper.CoownershipListingMapper;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.service.ListingService;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
@RestController
@RequestMapping("/listings")
public class ListingController {
    private final ListingService listingService;
    private final CoownershipListingMapper coownershipListingMapper;

    public ListingController(ListingService listingService, CoownershipListingMapper coownershipListingMapper) {
        this.listingService = listingService;
        this.coownershipListingMapper = coownershipListingMapper;
    }

    @PostMapping
    @Operation(
            summary = "Создать листинг совладения",
            description = "Если fundingDeadline не передан, сервис автоматически " +
                    "выставляет дедлайн на +90 дней от текущей даты."
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
}
