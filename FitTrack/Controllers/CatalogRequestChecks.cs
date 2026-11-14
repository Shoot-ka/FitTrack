using System.Text.Json;
using FitTrack.DTOs.Common;
using FluentValidation.Results;

namespace FitTrack.Controllers;

internal static class CatalogRequestChecks
{
    public static ErrorResponse? PaginationError(int page, int pageSize) =>
        page < 1 || pageSize < 1 || (long)(page - 1) * pageSize > int.MaxValue
            ? new ErrorResponse(400, "Provide a valid page and pageSize.")
            : null;

    public static ErrorResponse ValidationError(ValidationResult result)
    {
        var errors = result.Errors
            .GroupBy(error => JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

        return new ErrorResponse(400, "Validation failed", errors);
    }
}
