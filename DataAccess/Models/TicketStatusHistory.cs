namespace DataAccess.Models
{
    public class TicketStatusHistory
    {
        public int Id { get; set; }

        // FK → Ticket
        public int TicketId { get; set; }

        // FK → TicketStatus (الحالة السابقة)
        public int? FromStatusId { get; set; }

        // FK → TicketStatus (الحالة الجديدة)
        public int ToStatusId { get; set; }

        // FK → User (من غيّر الحالة)
        public int ChangedByUserId { get; set; }

        public DateTime ChangedAt { get; set; }

        // Navigation Properties
        public Ticket Ticket { get; set; } = null!;
        public TicketStatus? FromStatus { get; set; }
        public TicketStatus ToStatus { get; set; } = null!;
        public User ChangedByUser { get; set; } = null!;
    }
}
