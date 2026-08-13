namespace Contract.Dtos;

public class TicketStatisticsDto
{
    // عدد التذاكر حسب الحالة
    public IReadOnlyList<TicketStatusCountDto> ByStatus { get; init; }
        = new List<TicketStatusCountDto>();

    // عدد التذاكر المتأخرة
    public int OverdueCount { get; init; }

    // عدد التذاكر غير المعينة
    public int UnassignedCount { get; init; }

    // متوسط عدد الساعات لحل التذكرة
    public double? AverageResolutionHours { get; init; }

    // الإحصائيات حسب القسم
    public IReadOnlyList<TicketDepartmentStatisticsDto> ByDepartment { get; init; }
        = new List<TicketDepartmentStatisticsDto>();
}


