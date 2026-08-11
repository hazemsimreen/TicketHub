using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos
{
    public class CreateTicketDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }
    }
}