namespace DataAccess.Models;

public class NotificationType : AuditableEntity
{

    public string Code { get; set; } = string.Empty;

    public string TitleTemplate { get; set; } = string.Empty;

    public ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();
}