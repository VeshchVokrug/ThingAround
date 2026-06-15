package ru.veshvokrug.coownership.input.grpc;

import io.grpc.Status;
import io.grpc.StatusRuntimeException;
import io.grpc.stub.StreamObserver;
import jakarta.validation.Validation;
import jakarta.validation.Validator;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;
import ru.veshvokrug.coownership.grpc.*;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateRequestDto;
import ru.veshvokrug.coownership.input.dto.ShareApplicationCreateRequestDto;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.ShareApplication;
import ru.veshvokrug.coownership.service.ListingService;
import ru.veshvokrug.coownership.service.ServiceException;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

class CoownershipGrpcServiceTest {

    private jakarta.validation.ValidatorFactory validatorFactory;

    @BeforeEach
    void setUp() {
        validatorFactory = Validation.buildDefaultValidatorFactory();
    }

    @AfterEach
    void tearDown() {
        validatorFactory.close();
    }

    @Test
    void createListingMapsRequestWithoutNameDescriptionAndReturnsId() {
        ListingService listingService = mock(ListingService.class);
        Validator validator = validatorFactory.getValidator();
        CoownershipGrpcService grpcService = new CoownershipGrpcService(listingService, validator);

        UUID listingId = UUID.randomUUID();
        UUID catalogListingId = UUID.randomUUID();
        UUID ownerId = UUID.randomUUID();

        CoownershipListing listing = new CoownershipListing();
        listing.setId(listingId);
        when(listingService.createListing(any(CoownershipListingCreateRequestDto.class))).thenReturn(listing);

        CreateListingRequest request = CreateListingRequest.newBuilder()
                .setCatalogListingId(catalogListingId.toString())
                .setPrice("150000.00")
                .setOwnerId(ownerId.toString())
                .setTotalShares(6)
                .setFundingDeadline(LocalDate.now().plusDays(40).toString())
                .setTitle("Shared Camera")
                .setDescription("Camera for coownership")
                .setCategorySlug("electronics")
                .setCity("Moscow")
                .addImagesUrls("https://img.example/1.jpg")
                .build();

        @SuppressWarnings("unchecked")
        StreamObserver<CreateListingResponse> observer = mock(StreamObserver.class);

        grpcService.createListing(request, observer);

        ArgumentCaptor<CoownershipListingCreateRequestDto> dtoCaptor =
                ArgumentCaptor.forClass(CoownershipListingCreateRequestDto.class);
        verify(listingService).createListing(dtoCaptor.capture());
        CoownershipListingCreateRequestDto dto = dtoCaptor.getValue();
        assertThat(dto.catalogListingId()).isEqualTo(catalogListingId);
        assertThat(dto.ownerId()).isEqualTo(ownerId);
        assertThat(dto.price()).isEqualByComparingTo(new BigDecimal("150000.00"));
        assertThat(dto.totalShares()).isEqualTo(6);
        assertThat(dto.title()).isEqualTo("Shared Camera");
        assertThat(dto.categorySlug()).isEqualTo("electronics");
        assertThat(dto.city()).isEqualTo("Moscow");
        assertThat(dto.imagesUrls()).containsExactly("https://img.example/1.jpg");

        ArgumentCaptor<CreateListingResponse> responseCaptor = ArgumentCaptor.forClass(CreateListingResponse.class);
        verify(observer).onNext(responseCaptor.capture());
        assertThat(responseCaptor.getValue().getListingId()).isEqualTo(listingId.toString());
        verify(observer).onCompleted();
    }

    @Test
    void createListingAcceptsBlankFundingDeadlineAndUsesNullInDto() {
        ListingService listingService = mock(ListingService.class);
        Validator validator = validatorFactory.getValidator();
        CoownershipGrpcService grpcService = new CoownershipGrpcService(listingService, validator);

        CoownershipListing listing = new CoownershipListing();
        listing.setId(UUID.randomUUID());
        when(listingService.createListing(any(CoownershipListingCreateRequestDto.class))).thenReturn(listing);

        CreateListingRequest request = CreateListingRequest.newBuilder()
                .setCatalogListingId(UUID.randomUUID().toString())
                .setPrice("150000.00")
                .setOwnerId(UUID.randomUUID().toString())
                .setTotalShares(6)
                .setFundingDeadline("")
                .setTitle("Shared Camera")
                .setCategorySlug("electronics")
                .build();

        @SuppressWarnings("unchecked")
        StreamObserver<CreateListingResponse> observer = mock(StreamObserver.class);

        grpcService.createListing(request, observer);

        ArgumentCaptor<CoownershipListingCreateRequestDto> dtoCaptor = ArgumentCaptor
                .forClass(CoownershipListingCreateRequestDto.class);
        verify(listingService).createListing(dtoCaptor.capture());
        assertThat(dtoCaptor.getValue().fundingDeadline()).isNull();
        verify(observer).onCompleted();
    }

    @Test
    void createListingReturnsInvalidArgumentWhenOwnerIdIsNotUuid() {
        ListingService listingService = mock(ListingService.class);
        Validator validator = validatorFactory.getValidator();
        CoownershipGrpcService grpcService = new CoownershipGrpcService(listingService, validator);

        CreateListingRequest request = CreateListingRequest.newBuilder()
                .setCatalogListingId(UUID.randomUUID().toString())
                .setPrice("150000.00")
                .setOwnerId("not-a-uuid")
                .setTotalShares(6)
                .build();

        @SuppressWarnings("unchecked")
        StreamObserver<CreateListingResponse> observer = mock(StreamObserver.class);

        grpcService.createListing(request, observer);

        ArgumentCaptor<Throwable> errorCaptor = ArgumentCaptor.forClass(Throwable.class);
        verify(observer).onError(errorCaptor.capture());
        assertThat(errorCaptor.getValue()).isInstanceOf(StatusRuntimeException.class);
        StatusRuntimeException exception = (StatusRuntimeException) errorCaptor.getValue();
        assertThat(exception.getStatus().getCode()).isEqualTo(Status.Code.INVALID_ARGUMENT);
        verify(listingService, never()).createListing(any(CoownershipListingCreateRequestDto.class));
    }

    @Test
    void createListingReturnsInvalidArgumentWhenPriceIsInvalid() {
        ListingService listingService = mock(ListingService.class);
        Validator validator = validatorFactory.getValidator();
        CoownershipGrpcService grpcService = new CoownershipGrpcService(listingService, validator);

        CreateListingRequest request = CreateListingRequest.newBuilder()
                .setCatalogListingId(UUID.randomUUID().toString())
                .setPrice("not-a-number")
                .setOwnerId(UUID.randomUUID().toString())
                .setTotalShares(6)
                .build();

        @SuppressWarnings("unchecked")
        StreamObserver<CreateListingResponse> observer = mock(StreamObserver.class);

        grpcService.createListing(request, observer);

        ArgumentCaptor<Throwable> errorCaptor = ArgumentCaptor.forClass(Throwable.class);
        verify(observer).onError(errorCaptor.capture());
        assertThat(errorCaptor.getValue()).isInstanceOf(StatusRuntimeException.class);
        assertThat(((StatusRuntimeException) errorCaptor
                .getValue())
                .getStatus()
                .getCode())
                .isEqualTo(Status.Code.INVALID_ARGUMENT);
        verify(listingService, never()).createListing(any(CoownershipListingCreateRequestDto.class));
    }

    @Test
    void createShareApplicationMapsRequestAndReturnsResponse() {
        ListingService listingService = mock(ListingService.class);
        Validator validator = validatorFactory.getValidator();
        CoownershipGrpcService grpcService = new CoownershipGrpcService(listingService, validator);

        UUID listingId = UUID.randomUUID();
        UUID applicantId = UUID.randomUUID();
        ShareApplication application = new ShareApplication();
        application.setId(UUID.randomUUID());
        CoownershipListing listing = new CoownershipListing();
        listing.setId(listingId);
        application.setListing(listing);
        application.setApplicantId(applicantId);
        application.setSharesCount(2);
        when(listingService.createShareApplication(any(UUID.class),
                any(ShareApplicationCreateRequestDto.class)))
                .thenReturn(application);

        CreateShareApplicationRequest request = CreateShareApplicationRequest.newBuilder()
                .setListingId(listingId.toString())
                .setApplicantId(applicantId.toString())
                .setSharesCount(2)
                .build();

        @SuppressWarnings("unchecked")
        StreamObserver<ru.veshvokrug.coownership.grpc.ShareApplicationResponse> observer =
                mock(StreamObserver.class);

        grpcService.createShareApplication(request, observer);

        ArgumentCaptor<ShareApplicationCreateRequestDto> dtoCaptor =
                ArgumentCaptor.forClass(ShareApplicationCreateRequestDto.class);
        verify(listingService).createShareApplication(eq(listingId), dtoCaptor.capture());
        assertThat(dtoCaptor.getValue().applicantId()).isEqualTo(applicantId);
        assertThat(dtoCaptor.getValue().sharesCount()).isEqualTo(2);
        verify(observer).onCompleted();
    }

    @Test
    void createShareApplicationReturnsInvalidArgumentWhenSharesCountIsZero() {
        ListingService listingService = mock(ListingService.class);
        Validator validator = validatorFactory.getValidator();
        CoownershipGrpcService grpcService = new CoownershipGrpcService(listingService, validator);

        CreateShareApplicationRequest request = CreateShareApplicationRequest.newBuilder()
                .setListingId(UUID.randomUUID().toString())
                .setApplicantId(UUID.randomUUID().toString())
                .setSharesCount(0)
                .build();

        @SuppressWarnings("unchecked")
        StreamObserver<ru.veshvokrug.coownership.grpc.ShareApplicationResponse> observer =
                mock(StreamObserver.class);

        grpcService.createShareApplication(request, observer);

        ArgumentCaptor<Throwable> errorCaptor = ArgumentCaptor.forClass(Throwable.class);
        verify(observer).onError(errorCaptor.capture());
        assertThat(errorCaptor.getValue()).isInstanceOf(StatusRuntimeException.class);
        assertThat(((StatusRuntimeException)
                errorCaptor.getValue())
                .getStatus()
                .getCode())
                .isEqualTo(Status.Code.INVALID_ARGUMENT);
        verify(listingService, never()).createShareApplication(any(UUID.class),
                any(ShareApplicationCreateRequestDto.class));
    }

    @Test
    void approveShareApplicationMapsForbiddenServiceException() {
        ListingService listingService = mock(ListingService.class);
        Validator validator = validatorFactory.getValidator();
        CoownershipGrpcService grpcService = new CoownershipGrpcService(listingService, validator);

        UUID applicationId = UUID.randomUUID();
        UUID ownerId = UUID.randomUUID();
        when(listingService.approveShareApplicationByOwner(applicationId, ownerId))
                .thenThrow(ServiceException.forbidden("Подтверждать заявку может только владелец листинга"));

        OwnerActionRequest request = OwnerActionRequest.newBuilder()
                .setApplicationId(applicationId.toString())
                .setOwnerId(ownerId.toString())
                .build();

        @SuppressWarnings("unchecked")
        StreamObserver<ShareApplicationResponse> observer = mock(StreamObserver.class);

        grpcService.approveShareApplication(request, observer);

        ArgumentCaptor<Throwable> errorCaptor = ArgumentCaptor.forClass(Throwable.class);
        verify(observer).onError(errorCaptor.capture());
        assertThat(errorCaptor.getValue()).isInstanceOf(StatusRuntimeException.class);
        assertThat(((StatusRuntimeException) errorCaptor.getValue())
                .getStatus()
                .getCode())
                .isEqualTo(Status.Code.PERMISSION_DENIED);
        verify(listingService).approveShareApplicationByOwner(applicationId, ownerId);
    }
}
