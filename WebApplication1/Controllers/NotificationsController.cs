using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using Contract.Paged;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // GET: /api/notifications
    // Mine only — there is no user-id parameter, by design.
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ServiceResult<PagedResult<NotificationDto>>>> GetMine(
        [FromQuery] PagedQuery query,
        CancellationToken ct = default)
    {
        var result = await _notificationService.GetMineAsync(query, ct);
        return StatusCode(result.StatusCode, result);
    }

    // GET: /api/notifications/unread-count
    [HttpGet("unread-count")]
    [Authorize]
    public async Task<ActionResult<ServiceResult<UnreadCountDto>>> GetUnreadCount(
        CancellationToken ct = default)
    {
        var result = await _notificationService.GetUnreadCountAsync(ct);
        return StatusCode(result.StatusCode, result);
    }

    // POST: /api/notifications/{id}/read
    // Idempotent.
    [HttpPost("{id:guid}/read")]
    [Authorize]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct = default)
    {
        var result = await _notificationService.MarkReadAsync(id, ct);
        return StatusCode(result.StatusCode, result);
    }

    // POST: /api/notifications/read-all
    // One UPDATE with ExecuteUpdateAsync, not a loop.
    [HttpPost("read-all")]
    [Authorize]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
    {
        var result = await _notificationService.MarkAllReadAsync(ct);
        return StatusCode(result.StatusCode, result);
    }

    // DELETE: /api/notifications/{id}
    // Dismiss.
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _notificationService.DeleteAsync(id, ct);
        return StatusCode(result.StatusCode, result);
    }
}
