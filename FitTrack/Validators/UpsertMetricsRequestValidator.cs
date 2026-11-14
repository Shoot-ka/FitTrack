using FitTrack.DTOs.Metrics;
using FluentValidation;

namespace FitTrack.Validators;

public sealed class UpsertMetricsRequestValidator : AbstractValidator<UpsertMetricsRequest>
{
    public UpsertMetricsRequestValidator()
    {
        RuleFor(x => x.HeightCm).InclusiveBetween(100m, 250m);
        RuleFor(x => x.WeightKg).InclusiveBetween(30m, 300m);
        RuleFor(x => x.Goal).NotNull().IsInEnum();
    }
}
