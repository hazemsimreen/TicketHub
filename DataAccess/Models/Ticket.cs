using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public TicketCategory Category { get; set; }

        [Required]
        public string Priority { get; set; } = string.Empty;

        public TicketStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}