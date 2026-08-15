using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using DataAccess.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TicketHub.DataAccess.Repositories;
using Result = BusinessLogic.Common.ServiceResult;

namespace BusinessLogic.Services;

public class AttachmentService : IAttachmentService
{
    // Allow list, not a block list — a block list has to anticipate every
    // dangerous extension and will always be missing one.
    private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".pdf"];

    private const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly string _rootPath;

    public AttachmentService(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IWebHostEnvironment env,
        IConfiguration configuration)
    {
        _uow = uow;
        _currentUser = currentUser;

        var configuredPath = configuration["FileStorage:RootPath"] ?? "App_Data/uploads";

        _rootPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(env.ContentRootPath, configuredPath);

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<ServiceResult<IReadOnlyList<AttachmentDto>>> GetForTicketAsync(
        Guid ticketId, CancellationToken ct = default)
    {
        var canSee = await TicketVisibility
            .Apply(_uow.Repository<Ticket>().Query().AsNoTracking(), _currentUser)
            .AnyAsync(t => t.Id == ticketId, ct);

        if (!canSee)
        {
            return ServiceResult<IReadOnlyList<AttachmentDto>>.NotFound("Ticket not found.");
        }

        // Inline projection — EF can translate an object initializer to SQL, it
        // cannot translate a call to our own ToDto() helper.
        IReadOnlyList<AttachmentDto> items = await _uow.Repository<Attachment>()
            .Query()
            .AsNoTracking()
            .Where(a => a.TicketId == ticketId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttachmentDto
            {
                Id = a.Id,
                TicketId = a.TicketId ?? Guid.Empty,
                FileName = a.FileName,
                ContentType = a.ContentType,
                SizeBytes = a.SizeBytes,
                UploadedByUserId = a.UploadedByUserId,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<AttachmentDto>>.Success(items);
    }

    public async Task<ServiceResult<AttachmentDto>> UploadAsync(
        Guid ticketId, IFormFile file, CancellationToken ct = default)
    {
        if (_currentUser.UserId is null || !Guid.TryParse(_currentUser.UserId, out var uploaderId))
        {
            return ServiceResult<AttachmentDto>.Unauthorized("User is not authenticated.");
        }

        var ticket = await TicketVisibility
            .Apply(_uow.Repository<Ticket>().Query().AsNoTracking(), _currentUser)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null)
        {
            return ServiceResult<AttachmentDto>.NotFound("Ticket not found.");
        }

        if (file is null || file.Length == 0)
        {
            return ServiceResult<AttachmentDto>.BadRequest("A non-empty file is required.");
        }

        if (file.Length > MaxSizeBytes)
        {
            return ServiceResult<AttachmentDto>.BadRequest("File exceeds the 10 MB limit.");
        }

        // Never build a path from the caller's filename — extension check happens
        // on the extension alone, everything else about the name is display-only.
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            return ServiceResult<AttachmentDto>.BadRequest(
                $"File type '{extension}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}");
        }

        // Keep their name for DISPLAY. Generate our own name for the DISK.
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var relative = Path.Combine(ticketId.ToString(), storedName);
        var physicalDir = Path.Combine(_rootPath, ticketId.ToString());
        var physicalPath = Path.Combine(_rootPath, relative);

        Directory.CreateDirectory(physicalDir);

        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            StorageKey = relative,
            FileName = Path.GetFileName(file.FileName), // strip any directory part they sent
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            UploadedByUserId = uploaderId,
            CreatedBy = uploaderId.ToString()
        };

        await _uow.Repository<Attachment>().AddAsync(attachment, ct);
        await _uow.SaveChangesAsync(ct);

        return ServiceResult<AttachmentDto>.Created(ToDto(attachment));
    }

    public async Task<ServiceResult<AttachmentDownload>> DownloadAsync(
        Guid ticketId, Guid attachmentId, CancellationToken ct = default)
    {
        var attachment = await _uow.Repository<Attachment>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted, ct);

        // Check BOTH ids — an attachment id paired with a ticket the caller can see,
        // but that isn't the attachment's real ticket, must 404. That's the IDOR.
        if (attachment is null || attachment.TicketId != ticketId)
        {
            return ServiceResult<AttachmentDownload>.NotFound("Attachment not found.");
        }

        var canSee = await TicketVisibility
            .Apply(_uow.Repository<Ticket>().Query().AsNoTracking(), _currentUser)
            .AnyAsync(t => t.Id == ticketId, ct);

        if (!canSee)
        {
            return ServiceResult<AttachmentDownload>.NotFound("Attachment not found.");
        }

        var physicalPath = Path.Combine(_rootPath, attachment.StorageKey);

        if (!File.Exists(physicalPath))
        {
            return ServiceResult<AttachmentDownload>.NotFound("File is missing from storage.");
        }

        return ServiceResult<AttachmentDownload>.Success(new AttachmentDownload
        {
            PhysicalPath = physicalPath,
            ContentType = attachment.ContentType,
            FileName = attachment.FileName
        });
    }

    public async Task<Result> DeleteAsync(Guid ticketId, Guid attachmentId, CancellationToken ct = default)
    {
        if (!TicketVisibility.IsStaff(_currentUser))
        {
            return Result.Forbidden("Only staff can delete attachments.");
        }

        var attachmentRepo = _uow.Repository<Attachment>();

        var attachment = await attachmentRepo.Query()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

        if (attachment is null || attachment.TicketId != ticketId)
        {
            return Result.NotFound("Attachment not found.");
        }

        attachmentRepo.Remove(attachment); // soft delete
        await _uow.SaveChangesAsync(ct);

        try
        {
            var physicalPath = Path.Combine(_rootPath, attachment.StorageKey);
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
        catch
        {
            // Best-effort — the DB row (the source of truth) is already soft-deleted.
        }

        return Result.NoContent();
    }

    private static AttachmentDto ToDto(Attachment a) => new()
    {
        Id = a.Id,
        TicketId = a.TicketId ?? Guid.Empty,
        FileName = a.FileName,
        ContentType = a.ContentType,
        SizeBytes = a.SizeBytes,
        UploadedByUserId = a.UploadedByUserId,
        CreatedAt = a.CreatedAt
    };
}
