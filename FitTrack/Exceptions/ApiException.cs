using FitTrack.DTOs.Common;

namespace FitTrack.Exceptions;

public sealed class ApiException(ErrorResponse error) : Exception(error.Message)
{
    public ErrorResponse Error { get; } = error;
}
