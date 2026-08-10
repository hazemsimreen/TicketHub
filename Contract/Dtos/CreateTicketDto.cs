using DataAccess.Models;
using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos
{
    public class CreateTicketDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string Priority { get; set; } = string.Empty;
    }
}