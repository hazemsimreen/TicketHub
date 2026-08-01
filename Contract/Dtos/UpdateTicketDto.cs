using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos
{
    public class UpdateTicketDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int PriorityId { get; set; }
    }
}