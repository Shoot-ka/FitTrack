namespace FitTrack.DTOs.Common;

public sealed record ErrorResponse(
    int Status,
    string Message,
    IDictionary<string, string[]>? Errors = null);
