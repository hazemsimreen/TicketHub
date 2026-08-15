using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using Contract.Paged;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Supervisor")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    // GET: /api/reports/category-satisfaction
    // Group by category, average stars, count — paged.
    [HttpGet("category-satisfaction")]
    public async Task<ActionResult<ServiceResult<PagedResult<CategorySatisfactionDto>>>> CategorySatisfaction(
        [FromQuery] PagedQuery query,
        CancellationToken ct = default)
    {
        var result = await _reportService.CategorySatisfactionAsync(query, ct);
        return StatusCode(result.StatusCode, result);
    }

    // GET: /api/reports/daily-volume?from=2026-01-01&to=2026-01-31
    // Created vs resolved per day. Range is capped.
    [HttpGet("daily-volume")]
    public async Task<ActionResult<ServiceResult<List<DailyVolumeDto>>>> DailyVolume(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct = default)
    {
        var result = await _reportService.DailyVolumeAsync(from, to, ct);
        return StatusCode(result.StatusCode, result);
    }

    // GET: /api/reports/agent-workload
    // Per agent: open, resolved this month, average resolution time.
    [HttpGet("agent-workload")]
    public async Task<ActionResult<ServiceResult<List<AgentWorkloadDto>>>> AgentWorkload(
        CancellationToken ct = default)
    {
        var result = await _reportService.AgentWorkloadAsync(ct);
        return StatusCode(result.StatusCode, result);
    }
}
