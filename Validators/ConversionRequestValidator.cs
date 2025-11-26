using ApolloMigration.Models;
using FluentValidation;

namespace ApolloMigration.Validators;

public class ConversionRequestValidator : AbstractValidator<ConversionRequest>
{
    public ConversionRequestValidator()
    {
        RuleFor(x => x.SourceDocumentType)
            .NotEmpty()
            .WithMessage("Source document type is required")
            .MaximumLength(100)
            .WithMessage("Source document type cannot exceed 100 characters");

        RuleFor(x => x.TargetDocumentType)
            .NotEmpty()
            .WithMessage("Target document type is required")
            .MaximumLength(100)
            .WithMessage("Target document type cannot exceed 100 characters");

        RuleFor(x => x.BatchSize)
            .GreaterThan(0)
            .WithMessage("Batch size must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Batch size cannot exceed 1000");

        RuleFor(x => x.FilterExpression)
            .MaximumLength(500)
            .WithMessage("Filter expression cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.FilterExpression));
    }
}
