using System.Security.Claims;
using System.Text.Json;
using FitTrack.DTOs.Common;
using FitTrack.DTOs.Metrics;
using FitTrack.Enums;
using FitTrack.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Member))]
[Route("api/me/metrics")]
public sealed class BodyMetricsController(
    IBodyMetricsService service,
    IValidator<UpsertMetricsRequest> validator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BodyMetricsDto>> Get(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue("sub"), out var userId))
        {
            return Unauthorized();
        }

        var metrics = await service.GetAsync(userId, cancellationToken);
        if (metrics is null)
        {
            return NotFound(new ErrorResponse(404, "Body metrics have not been recorded."));
        }

        return Ok(metrics);
    }

    [HttpPut]
    public async Task<ActionResult<BodyMetricsDto>> Upsert(
        UpsertMetricsRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue("sub"), out var userId))
        {
            return Unauthorized();
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(error => JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            return BadRequest(new ErrorResponse(400, "Validation failed", errors));
        }

        return Ok(await service.UpsertAsync(userId, request, cancellationToken));
    }
}
