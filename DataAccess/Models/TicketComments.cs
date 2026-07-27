namespace DataAccess.Models;

public class TicketComments
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int AuthorUserId { get; set; }

    public int? StepInstanceId { get; set; }

    public int? ParentCommentId { get; set; }
}