using System.Security.Claims;
using FitTrack.DTOs.Stats;
using FitTrack.Enums;
using FitTrack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Member))]
[Route("api/stats")]
public sealed class StatsController(IStatsService stats) : ControllerBase
{
    [HttpGet("weekly")]
    public async Task<ActionResult<IReadOnlyList<WeeklyStatsDto>>> GetWeekly(CancellationToken ct, int weeks = 4) =>
        Ok(await stats.GetWeeklyAsync(UserId, weeks, ct));

    [HttpGet("exercises/{exerciseId:int}")]
    public async Task<ActionResult<IReadOnlyList<ExerciseProgressDto>>> GetExerciseProgress(
        int exerciseId, CancellationToken ct) =>
        Ok(await stats.GetExerciseProgressAsync(UserId, exerciseId, ct));

    private int UserId => int.Parse(User.FindFirstValue("sub")!);
}
