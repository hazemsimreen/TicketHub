using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos
{
    public class CreateCommentDto
    {
        [Required]
        public string Author { get; set; } = string.Empty;

        [Required]
        public string Text { get; set; } = string.Empty;
    }
}