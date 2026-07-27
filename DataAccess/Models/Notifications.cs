namespace DataAccess.Models;

public class Notifications
{
    public int Id { get; set; }

    public int RecipientUserId { get; set; }

    public int NotificationTypeId { get; set; }

    public int? TicketId { get; set; }

    public bool IsRead { get; set; }
}