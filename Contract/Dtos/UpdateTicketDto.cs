using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos;

public class UpdateTicketDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int PriorityId { get; set; }

    /// <summary>
    /// RowVersion المُستلمة من العميل (Base64) — تُستخدم للتحقق من التعارض (Optimistic Concurrency)
    /// </summary>
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}