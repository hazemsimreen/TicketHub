namespace DataAccess.Models
{
    public class TicketStatus
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        // هل هذا الـ Status نهائي؟ (Resolved / Closed)
        public bool IsTerminal { get; set; }
    }
}