namespace Contract.Dtos;

public class AssignTicketDto
{
    /// <summary>
    /// null = إلغاء التعيين (Un-assign)
    /// </summary>
    public Guid? AssignedToUserId { get; set; }
}