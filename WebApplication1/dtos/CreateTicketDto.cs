using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

namespace WebApplication1.Dtos
{
    public class CreateTicketDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [EnumDataType(typeof(TicketCategory))]
        public TicketCategory Category { get; set; }

        [Required]
        public string Priority { get; set; } = string.Empty;
    }
}