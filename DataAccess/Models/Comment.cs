using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int TicketId { get; set; }

        [Required]
        public string Author { get; set; } = string.Empty;

        [Required]
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}