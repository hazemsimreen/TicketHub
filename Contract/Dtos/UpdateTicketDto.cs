using DataAccess.Models;
using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos
{
    public class UpdateTicketDto
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