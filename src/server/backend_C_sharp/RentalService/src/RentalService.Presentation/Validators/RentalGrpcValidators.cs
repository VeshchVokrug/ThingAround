using FluentValidation;
using RentalService.Grpc;

namespace RentalService.Presentation.Validators;

public class CalendarDateValidator : AbstractValidator<CalendarDate>
{
    public CalendarDateValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2024, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Day).InclusiveBetween(1, 31);

        RuleFor(x => x).Must(BeValidDate).WithMessage("Invalid date values");
    }

    private bool BeValidDate(CalendarDate date)
    {
        try
        {
            var _ = new DateOnly(date.Year, date.Month, date.Day);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.ListingId)
            .Must(BeAValidGuid).WithMessage("ListingId must be a valid GUID");

        RuleFor(x => x.OwnerId)
            .Must(BeAValidGuid).WithMessage("OwnerId must be a valid GUID");

        RuleFor(x => x.StartDate)
            .NotNull()
            .SetValidator(new CalendarDateValidator());

        RuleFor(x => x.EndDate)
            .NotNull()
            .SetValidator(new CalendarDateValidator());
        
        RuleFor(x => x).Must(x => {
            var start = new DateOnly(x.StartDate.Year, x.StartDate.Month, x.StartDate.Day);
            var end = new DateOnly(x.EndDate.Year, x.EndDate.Month, x.EndDate.Day);
            return end > start;
        }).WithMessage("End date must be after start date");

        RuleFor(x => x.ExpectedPrice)
            .GreaterThan(0).WithMessage("Price must be greater than 0");
    }

    private bool BeAValidGuid(string guid) => Guid.TryParse(guid, out _);
}

public class GetBookingByIdRequestValidator : AbstractValidator<GetBookingByIdRequest>
{
    public GetBookingByIdRequestValidator()
    {
        RuleFor(x => x.BookingId)
            .Must(guid => Guid.TryParse(guid, out _))
            .WithMessage("Invalid Booking ID format");
    }
}

public class GetByUserIdRequestValidator : AbstractValidator<GetByUserIdRequest>
{
    public GetByUserIdRequestValidator()
    {
        RuleFor(x => x.UserId)
            .Must(guid => Guid.TryParse(guid, out _))
            .WithMessage("Invalid User ID format");
    }
}

public class ChangeStatusWithReasonRequestValidator : AbstractValidator<ChangeStatusWithReasonRequest>
{
    public ChangeStatusWithReasonRequestValidator()
    {
        RuleFor(x => x.BookingId)
            .Must(guid => Guid.TryParse(guid, out _))
            .WithMessage("Invalid Booking ID format");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MinimumLength(5).WithMessage("Reason must be at least 5 characters long")
            .MaximumLength(500).WithMessage("Reason is too long");
    }
}