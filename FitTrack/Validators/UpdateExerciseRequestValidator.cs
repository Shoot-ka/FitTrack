using FitTrack.DTOs.Catalog;
using FluentValidation;

namespace FitTrack.Validators;

public sealed class UpdateExerciseRequestValidator : AbstractValidator<UpdateExerciseRequest>
{
    public UpdateExerciseRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty()
            .Must(name => name is not null && name.Trim().Length is >= 2 and <= 80)
            .WithMessage("Name must be between 2 and 80 characters.");
        RuleFor(request => request.Description).MaximumLength(500);
        RuleFor(request => request.Equipment).NotNull().IsInEnum();
        RuleFor(request => request.MuscleGroupIds).NotEmpty()
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Muscle group IDs must be unique.");
    }
}
