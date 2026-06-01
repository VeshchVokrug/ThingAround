using Core.Contracts;
using FluentValidation;
using IdentityProfileService.Grpc;

namespace IdentityProfileService.Validators;

public class CategoriesRequestValidator : AbstractValidator<CategoriesRequest>
{
    public CategoriesRequestValidator()
    {
        RuleFor(x => x.Categories)
            .NotNull()
            .WithMessage("Categories list cannot be null");
        
        RuleForEach(x => x.Categories)
            .Must(BeAValidCategory)
            .WithMessage(category => $"Category {category} is invalid");
    }

    private bool BeAValidCategory(string value)
    {
        return Category.FromValue(value) != null;
    }
}