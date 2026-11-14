using FitTrack.DTOs.Catalog;
using FluentValidation;

namespace FitTrack.Validators;

public sealed class CreateMuscleGroupRequestValidator : AbstractValidator<CreateMuscleGroupRequest>
{
    public CreateMuscleGroupRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(80);
    }
}
