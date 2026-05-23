using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using RentalService.Application.Services.Abstractions;
using RentalService.Grpc;
using RentalService.Presentation.Mapper;

namespace RentalService.Presentation.gRPC;

public class GrpcRentalService : RentalService.Grpc.RentalService.RentalServiceBase
{
    private readonly IBookingService _bookingService;

    public GrpcRentalService(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    public override async Task<CreateBookingResponse> CreateBooking(CreateBookingRequest request, ServerCallContext context)
    {
        var response = await _bookingService.CreateAsync(request.ToDto(), context.CancellationToken); 
        return response.ToGrpc();
    }

    public override async Task<Booking> GetBooking(GetBookingByIdRequest request, ServerCallContext context)
    {
        var id = ParseGuidOrThrow(request.BookingId);
        var booking = await _bookingService.GetAsync(id);
        return booking.ToGrpc();
    }

    public override async Task<BookingListResponse> GetBookingsByTenant(Empty request, ServerCallContext context)
    {
        var bookings = await _bookingService.GetAllByTenantAsync();
        return bookings.ToGrpc();
    }

    public override async Task<BookingListResponse> GetBookingsByOwner(Empty request, ServerCallContext context)
    {
        var bookings = await _bookingService.GetAllByOwnerAsync();
        return bookings.ToGrpc();
    }

    public override async Task<BookingListResponse> GetCompletedBookingsByTenant(Empty request, ServerCallContext context)
    {
        var bookings = await _bookingService.GetAllCompletedByTenantAsync();
        return bookings.ToGrpc();
    }

    public override async Task<BookingListResponse> GetCompletedBookingsByOwner(Empty request, ServerCallContext context)
    {
        var bookings = await _bookingService.GetAllCompletedByOwnerAsync();
        return bookings.ToGrpc();
    }

    public override async Task<ApprovalResponse> ApproveBooking(GetBookingByIdRequest request, ServerCallContext context)
    {
        var bookingId = ParseGuidOrThrow(request.BookingId);
        var result = await _bookingService.ApproveBookingAsync(bookingId, context.CancellationToken);
        return result.ToGrpc();
    }

    public override async Task<ApprovalResponse> RejectBooking(ChangeStatusWithReasonRequest request, ServerCallContext context)
    {
        var bookingId = ParseGuidOrThrow(request.BookingId);
        var result = await _bookingService.RejectBookingAsync(bookingId, request.Reason, context.CancellationToken);
        return result.ToGrpc();
    }

    public override async Task<ApprovalResponse> CancelBooking(ChangeStatusWithReasonRequest request, ServerCallContext context)
    {
        var bookingId = ParseGuidOrThrow(request.BookingId);
        var result = await _bookingService.CancelBookingAsync(bookingId, request.Reason, context.CancellationToken);
        return result.ToGrpc();
    }

    private static Guid ParseGuidOrThrow(string value)
    {
        if (Guid.TryParse(value, out var guid))
        {
            return guid;
        }

        throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid GUID format: {value}"));
    }
}