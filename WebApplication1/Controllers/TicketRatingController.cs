using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:guid}/rating")]
public class TicketRatingController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public TicketRatingController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    // POST: /api/tickets/{ticketId}/rating
    // Reporter only. Resolved tickets only. One per ticket — unique index backs the 409.
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ServiceResult<RatingDto>>> AddRating(
        Guid ticketId,
        [FromBody] AddRatingDto dto,
        CancellationToken ct = default)
    {
        var result = await _ratingService.AddAsync(ticketId, dto, ct);
        return StatusCode(result.StatusCode, result);
    }

    // GET: /api/tickets/{ticketId}/rating
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ServiceResult<RatingDto>>> GetRating(
        Guid ticketId,
        CancellationToken ct = default)
    {
        var result = await _ratingService.GetForTicketAsync(ticketId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
