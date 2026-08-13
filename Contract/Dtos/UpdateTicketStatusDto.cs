using System.ComponentModel.DataAnnotations;

namespace Contract.Dtos;

public class UpdateTicketStatusDto
{
    [Required]
    public string NewStatusCode { get; set; } = string.Empty;

    /// <summary>
    /// إلزامي فقط عند الإلغاء (Cancelled) — التحقق يتم بالـ Service
    /// </summary>
    public string? Reason { get; set; }
}