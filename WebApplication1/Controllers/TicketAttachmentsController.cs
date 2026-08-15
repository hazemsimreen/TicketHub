using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:guid}/attachments")]
public class TicketAttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachmentService;

    public TicketAttachmentsController(IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    // GET: /api/tickets/{ticketId}/attachments
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ServiceResult<IReadOnlyList<AttachmentDto>>>> GetAttachments(
        Guid ticketId,
        CancellationToken ct = default)
    {
        var result = await _attachmentService.GetForTicketAsync(ticketId, ct);
        return StatusCode(result.StatusCode, result);
    }

    // POST: /api/tickets/{ticketId}/attachments
    // multipart/form-data. RequestSizeLimit rejects an oversized body before
    // Kestrel even lets us read a byte — the service-level size check is
    // defense in depth for anything that slips under that limit.
    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ServiceResult<AttachmentDto>>> Upload(
        Guid ticketId,
        IFormFile file,
        CancellationToken ct = default)
    {
        var result = await _attachmentService.UploadAsync(ticketId, file, ct);
        return StatusCode(result.StatusCode, result);
    }

    // GET: /api/tickets/{ticketId}/attachments/{attachmentId}
    // Streams the file. Checks BOTH ids — see AttachmentService.DownloadAsync.
    [HttpGet("{attachmentId:guid}")]
    [Authorize]
    public async Task<IActionResult> Download(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken ct = default)
    {
        var result = await _attachmentService.DownloadAsync(ticketId, attachmentId, ct);

        if (!result.IsSuccess || result.Data is null)
        {
            return StatusCode(result.StatusCode, result);
        }

        var stream = System.IO.File.OpenRead(result.Data.PhysicalPath);
        return File(stream, result.Data.ContentType, result.Data.FileName);
    }

    // DELETE: /api/tickets/{ticketId}/attachments/{attachmentId}
    // Staff only.
    [HttpDelete("{attachmentId:guid}")]
    [Authorize(Roles = "Admin,Supervisor,Agent")]
    public async Task<IActionResult> Delete(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken ct = default)
    {
        var result = await _attachmentService.DeleteAsync(ticketId, attachmentId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
