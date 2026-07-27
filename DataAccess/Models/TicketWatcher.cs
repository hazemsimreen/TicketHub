namespace DataAccess.Models
{
    public class TicketWatcher
    {
        public int Id { get; set; }

        // FK → Ticket
        public int TicketId { get; set; }

        // FK → User (المستخدم اللي يتابع التذكرة)
        public int UserId { get; set; }

        // Navigation Properties
        public Ticket Ticket { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
