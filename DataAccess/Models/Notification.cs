namespace DataAccess.Models;

public class Notification : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid RecipientUserId { get; set; }

    public Guid NotificationTypeId { get; set; }

    public Guid? TicketId { get; set; }

    public bool IsRead { get; set; } = false;

    public User RecipientUser { get; set; } = null!;

    public NotificationType NotificationType { get; set; } = null!;

    public Ticket? Ticket { get; set; }
}