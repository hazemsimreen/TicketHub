namespace DataAccess.Models;

public class NotificationTypes
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string TitleTemplate { get; set; } = string.Empty;
}