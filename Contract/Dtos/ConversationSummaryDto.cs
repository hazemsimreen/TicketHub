namespace Contract.Dtos;

public class ConversationSummaryDto
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }

    public string TicketTitle { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string? LastMessage { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public int UnreadCount { get; set; }
}