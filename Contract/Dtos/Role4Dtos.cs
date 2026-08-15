namespace Contract.Dtos;

// Comments

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string AuthorNameSnapshot { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddCommentDto
{
    public string Body { get; set; } = string.Empty;

    /// <summary>Ignored (forced false) when the caller is a Citizen — see CommentService.AddAsync.</summary>
    public bool IsInternal { get; set; }
}

public class EditCommentDto
{
    public string Body { get; set; } = string.Empty;
}

// Attachments

public class AttachmentDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>What the service hands the controller so it can stream the file — never a raw path to the client.</summary>
public class AttachmentDownload
{
    public string PhysicalPath { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
}

// Rating

public class RatingDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public Guid RatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddRatingDto
{
    public int Stars { get; set; }
    public string? Comment { get; set; }
}

// Notifications

public class NotificationDto
{
    public Guid Id { get; set; }
    public string NotificationTypeCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? TicketId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UnreadCountDto
{
    public int Count { get; set; }
}

// Reports

public class CategorySatisfactionDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double AverageStars { get; set; }
    public int RatingCount { get; set; }
}

public class DailyVolumeDto
{
    public DateOnly Date { get; set; }
    public int CreatedCount { get; set; }
    public int ResolvedCount { get; set; }
}

public class AgentWorkloadDto
{
    public int AgentId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public int OpenCount { get; set; }
    public int ResolvedThisMonthCount { get; set; }

    /// <summary>Null when the agent has no resolved tickets yet — there's nothing to average.</summary>
    public double? AverageResolutionHours { get; set; }
}
