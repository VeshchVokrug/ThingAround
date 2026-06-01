using MassTransit.Initializers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RentalService.Application.DTO;
using RentalService.Application.SAGA;
using RentalService.Domain.Entity;
using RentalService.Infrastructure.Abstractions.DTO;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;
using RentalService.Infrastructure.Persistence;

namespace RentalService.Infrastructure.Repository;

public class BookingStatesRepository : IBookingStatesRepository
{
    private readonly RentalDbContext _context;

    public BookingStatesRepository(RentalDbContext context)
    {
        _context = context;
    }

    public async Task<BookingStatusDto?> GetStatusAsync(Guid bookingId)
    {
        var state = await _context.BookingStates
            .FirstOrDefaultAsync(bs => bs.CorrelationId == bookingId);

        return state == null 
            ? null 
            : new BookingStatusDto(state.Status, state?.FailureReason);
    }
}
