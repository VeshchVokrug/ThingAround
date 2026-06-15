package ru.veshvokrug.coownership.input.grpc;

import io.grpc.stub.StreamObserver;
import jakarta.validation.ConstraintViolation;
import jakarta.validation.Validator;
import org.springframework.stereotype.Component;
import ru.veshvokrug.coownership.grpc.*;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.input.dto.ShareApplicationCreateRequestDto;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.service.ListingService;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;

/**
 * gRPC-адаптер для операций с листингами, заявками и уведомлениями.
 *
 * @author Dmitrii Marchenko 25.04.2026
 */
@Component
public class CoownershipGrpcService extends CoownershipServiceGrpc.CoownershipServiceImplBase {
    private final ListingService listingService;
    private final Validator validator;

    public CoownershipGrpcService(ListingService listingService, Validator validator) {
        this.listingService = listingService;
        this.validator = validator;
    }

    @Override
    public void createListing(CreateListingRequest request,
                              StreamObserver<CreateListingResponse> responseObserver) {
        handle(responseObserver, () -> {
            CoownershipListingCreateRequestDto dto = new CoownershipListingCreateRequestDto(
                    parseUuid(request.getCatalogListingId(), "catalog_listing_id"),
                    parseBigDecimal(request.getPrice()),
                    parseUuid(request.getOwnerId(), "owner_id"),
                    request.getTotalShares(),
                    request.getFundingDeadline().isBlank() ? null : parseDate(request.getFundingDeadline()),
                    request.getTitle(),
                    request.getDescription(),
                    request.getCategorySlug(),
                    request.getCity(),
                    request.getImagesUrlsList()
            );
            validate(dto);
            CoownershipListing listing = listingService.createListing(dto);
            return CreateListingResponse.newBuilder().setListingId(listing.getId().toString()).build();
        });
    }

    @Override
    public void createShareApplication(
            CreateShareApplicationRequest request,
            StreamObserver<ShareApplicationResponse> responseObserver) {
        handle(responseObserver, () -> {
            UUID listingId = parseUuid(request.getListingId(), "listing_id");
            ShareApplicationCreateRequestDto dto = new ShareApplicationCreateRequestDto(
                    parseUuid(request.getApplicantId(), "applicant_id"),
                    request.getSharesCount()
            );
            validate(dto);
            ShareApplication application = listingService.createShareApplication(listingId, dto);
            return GrpcModelMapper.toShareApplicationResponse(application);
        });
    }

    @Override
    public void approveShareApplication(
            OwnerActionRequest request,
            StreamObserver<ShareApplicationResponse> responseObserver) {
        handle(responseObserver, () -> {
            ShareApplication application = listingService.approveShareApplicationByOwner(
                    parseUuid(request.getApplicationId(), "application_id"),
                    parseUuid(request.getOwnerId(), "owner_id")
            );
            return GrpcModelMapper.toShareApplicationResponse(application);
        });
    }

    @Override
    public void rejectShareApplication(
            OwnerActionRequest request,
            StreamObserver<ShareApplicationResponse> responseObserver) {
        handle(responseObserver, () -> {
            ShareApplication application = listingService.rejectShareApplicationByOwner(
                    parseUuid(request.getApplicationId(), "application_id"),
                    parseUuid(request.getOwnerId(), "owner_id")
            );
            return GrpcModelMapper.toShareApplicationResponse(application);
        });
    }

    @Override
    public void getOwnerNotifications(
            GetOwnerNotificationsRequest request,
            StreamObserver<GetOwnerNotificationsResponse> responseObserver) {
        handle(responseObserver, () -> GrpcModelMapper.toOwnerNotificationsResponse(
                listingService.getOwnerNotifications(parseUuid(request.getOwnerId(), "owner_id"))
        ));
    }

    private <T> void handle(StreamObserver<T> observer, GrpcAction<T> action) {
        try {
            T response = action.execute();
            observer.onNext(response);
            observer.onCompleted();
        } catch (Throwable throwable) {
            observer.onError(GrpcExceptionMapper.toStatus(throwable));
        }
    }

    private void validate(Object dto) {
        Set<ConstraintViolation<Object>> violations = validator.validate(dto);
        if (!violations.isEmpty()) {
            String message = violations.stream()
                    .map(v -> v.getPropertyPath() + " " + v.getMessage())
                    .collect(Collectors.joining("; "));
            throw new IllegalArgumentException(message);
        }
    }

    private UUID parseUuid(String value, String field) {
        try {
            return UUID.fromString(value);
        } catch (Exception ex) {
            throw new IllegalArgumentException("Invalid UUID for field '" + field + "'");
        }
    }

    private BigDecimal parseBigDecimal(String value) {
        try {
            return new BigDecimal(value);
        } catch (Exception ex) {
            throw new IllegalArgumentException("Invalid decimal for field 'price'");
        }
    }

    private LocalDate parseDate(String value) {
        try {
            return LocalDate.parse(value);
        } catch (Exception ex) {
            throw new IllegalArgumentException("Invalid date for field 'funding_deadline', expected yyyy-MM-dd");
        }
    }

    @FunctionalInterface
    private interface GrpcAction<T> {
        T execute();
    }
}
