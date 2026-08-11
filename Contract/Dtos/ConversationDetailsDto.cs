namespace Contract.Dtos;

public class ConversationDetailsDto
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }

    public string TicketTitle { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<ConversationParticipantDto> Participants { get; set; } = [];
}

public class ConversationParticipantDto
{
    public Guid UserId { get; set; }

    public DateTime? LastReadAt { get; set; }
}