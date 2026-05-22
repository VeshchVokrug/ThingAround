using CatalogService.Grpc;
using Core.Contracts;
using FluentValidation;
using System.Globalization;

namespace Presentation.Validators;

public class CalendarDateValidator : AbstractValidator<CalendarDate>
{
    public CalendarDateValidator()
    {
        RuleFor(x => x)
            .NotNull()
            .WithMessage("Date must be provided")
            .Must(BeValidDate)
            .WithMessage("Date is invalid");
    }

    private static bool BeValidDate(CalendarDate? date)
    {
        if (date == null)
        {
            return false;
        }

        var text = $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}";
        return DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }
}

public class GetRentalListingRequestValidator : AbstractValidator<GetRentalListingRequest>
{
    public GetRentalListingRequestValidator()
    {
        RuleFor(x => x.ListingId)
            .NotEmpty()
            .WithMessage("ListingId is required")
            .Must(BeValidGuid)
            .WithMessage("ListingId must be a valid GUID");
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}

public class GetRentalListingsByUserRequestValidator : AbstractValidator<GetRentalListingsByUserRequest>
{
    public GetRentalListingsByUserRequestValidator()
    {
        RuleFor(x => x.OwnerId)
            .NotEmpty()
            .WithMessage("OwnerId is required")
            .Must(BeValidGuid)
            .WithMessage("OwnerId must be a valid GUID");
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}

public class CreateRentalListingRequestValidator : AbstractValidator<CreateRentalListingRequest>
{
    public CreateRentalListingRequestValidator()
    {
        RuleFor(x => x.CategorySlug)
            .NotEmpty()
            .WithMessage("CategorySlug is required")
            .Must(BeAValidCategory)
            .WithMessage(category => $"Category {category} is invalid");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required");

        RuleFor(x => x.DefaultPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DefaultPrice must be non-negative");

        RuleFor(x => x.ManagerId)
            .NotEmpty()
            .WithMessage("ManagerId is required")
            .Must(BeValidGuid)
            .WithMessage("ManagerId must be a valid GUID");

        RuleFor(x => x.ManagerName)
            .NotEmpty()
            .WithMessage("ManagerName is required");

        RuleFor(x => x.ManagerPhone)
            .NotEmpty()
            .WithMessage("ManagerPhone is required");

        RuleForEach(x => x.BusyDates)
            .SetValidator(new CalendarDateValidator());
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);

    private static bool BeAValidCategory(string value)
    {
        return Category.FromValue(value) != null;
    }
}

public class RentalFilterRequestValidator : AbstractValidator<RentalFilterRequest>
{
    public RentalFilterRequestValidator()
    {
        When(x => x.HasCategorySlug, () =>
        {
            RuleFor(x => x.CategorySlug)
                .NotEmpty()
                .WithMessage("CategorySlug cannot be empty")
                .Must(BeAValidCategory)
                .WithMessage(category => $"Category {category} is invalid");
        });

        When(x => x.HasMinPrice, () =>
        {
            RuleFor(x => x.MinPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("MinPrice must be non-negative");
        });

        When(x => x.HasMaxPrice, () =>
        {
            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("MaxPrice must be non-negative");
        });

        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("PageNumber must be greater than zero");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize must be greater than zero");

        When(x => x.StartDate != null, () =>
        {
            RuleFor(x => x.StartDate)
                .SetValidator(new CalendarDateValidator());
        });

        When(x => x.EndDate != null, () =>
        {
            RuleFor(x => x.EndDate)
                .SetValidator(new CalendarDateValidator());
        });

        When(x => x.StartDate != null && x.EndDate != null, () =>
        {
            RuleFor(x => x)
                .Must(HaveValidDateRange)
                .WithMessage("StartDate must be before or equal to EndDate");
        });
    }

    private static bool BeAValidCategory(string value)
    {
        return Category.FromValue(value) != null;
    }

    private static bool HaveValidDateRange(RentalFilterRequest request)
    {
        var start = request.StartDate;
        var end = request.EndDate;
        if (start == null || end == null)
        {
            return true;
        }

        var startDate = new DateOnly(start.Year, start.Month, start.Day);
        var endDate = new DateOnly(end.Year, end.Month, end.Day);
        return startDate <= endDate;
    }
}

public class ReservationSlotsRequestValidator : AbstractValidator<ReservationSlotsRequest>
{
    public ReservationSlotsRequestValidator()
    {
        RuleFor(x => x.ListingId)
            .NotEmpty()
            .WithMessage("ListingId is required")
            .Must(BeValidGuid)
            .WithMessage("ListingId must be a valid GUID");

        RuleFor(x => x.Dates)
            .NotNull()
            .WithMessage("Dates are required")
            .Must(dates => dates.Count > 0)
            .WithMessage("At least one date is required");

        RuleForEach(x => x.Dates)
            .SetValidator(new CalendarDateValidator());
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}

public class RentalListingValidator : AbstractValidator<RentalListing>
{
    public RentalListingValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required")
            .Must(BeValidGuid)
            .WithMessage("Id must be a valid GUID");

        RuleFor(x => x.CategorySlug)
            .NotEmpty()
            .WithMessage("CategorySlug is required")
            .Must(BeAValidCategory)
            .WithMessage(category => $"Category {category} is invalid");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required");

        RuleFor(x => x.DefaultPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DefaultPrice must be non-negative");

        RuleFor(x => x.OwnerId)
            .NotEmpty()
            .WithMessage("OwnerId is required")
            .Must(BeValidGuid)
            .WithMessage("OwnerId must be a valid GUID");

        RuleForEach(x => x.AvailabilitySlots)
            .SetValidator(new AvailabilitySlotValidator());
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);

    private static bool BeAValidCategory(string value)
    {
        return Category.FromValue(value) != null;
    }
}

public class AvailabilitySlotValidator : AbstractValidator<AvailabilitySlot>
{
    public AvailabilitySlotValidator()
    {
        RuleFor(x => x.Date)
            .SetValidator(new CalendarDateValidator());

        RuleFor(x => x.Version)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Version must be non-negative");

        When(x => x.HasBookingId, () =>
        {
            RuleFor(x => x.BookingId)
                .Must(BeValidGuid)
                .WithMessage("BookingId must be a valid GUID");
        });
    }

    private static bool BeValidGuid(string value) => Guid.TryParse(value, out _);
}


