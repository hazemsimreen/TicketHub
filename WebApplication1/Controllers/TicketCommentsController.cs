using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using Contract.Paged;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:guid}/comments")]
public class TicketCommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public TicketCommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    // GET: /api/tickets/{ticketId}/comments
    // Any authenticated user — internal notes are stripped in SQL for citizens
    // inside CommentService, not here.
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ServiceResult<PagedResult<CommentDto>>>> GetComments(
        Guid ticketId,
        [FromQuery] PagedQuery query,
        CancellationToken ct = default)
    {
        var result = await _commentService.GetForTicketAsync(ticketId, query, ct);
        return StatusCode(result.StatusCode, result);
    }

    // POST: /api/tickets/{ticketId}/comments
    // Any authenticated user — author comes from the token, isInternal forced
    // false for citizens.
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ServiceResult<CommentDto>>> AddComment(
        Guid ticketId,
        [FromBody] AddCommentDto dto,
        CancellationToken ct = default)
    {
        var result = await _commentService.AddAsync(ticketId, dto, ct);
        return StatusCode(result.StatusCode, result);
    }

    // PUT: /api/tickets/{ticketId}/comments/{commentId}
    // Author or Admin — ownership is checked on the row, inside CommentService.
    [HttpPut("{commentId:guid}")]
    [Authorize]
    public async Task<ActionResult<ServiceResult<CommentDto>>> UpdateComment(
        Guid ticketId,
        Guid commentId,
        [FromBody] EditCommentDto dto,
        CancellationToken ct = default)
    {
        var result = await _commentService.UpdateAsync(ticketId, commentId, dto, ct);
        return StatusCode(result.StatusCode, result);
    }

    // DELETE: /api/tickets/{ticketId}/comments/{commentId}
    // Author or Admin — soft delete.
    [HttpDelete("{commentId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(
        Guid ticketId,
        Guid commentId,
        CancellationToken ct = default)
    {
        var result = await _commentService.DeleteAsync(ticketId, commentId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
