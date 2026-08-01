namespace DataAccess.Models;

public class Notification
{
    public int Id { get; set; }

    public int RecipientUserId { get; set; }

    public int NotificationTypeId { get; set; }

    public int? TicketId { get; set; }

    public bool IsRead { get; set; }

    public User RecipientUser { get; set; } = null!;

    public NotificationType NotificationType { get; set; } = null!;

    public Ticket? Ticket { get; set; }
}