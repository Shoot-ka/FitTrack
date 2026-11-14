using FitTrack.DTOs.Trainer;
using FluentValidation;

namespace FitTrack.Validators;

public sealed class AddClientRequestValidator : AbstractValidator<AddClientRequest>
{
    public AddClientRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().MaximumLength(254).EmailAddress();
    }
}
