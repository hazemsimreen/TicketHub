using System;
using System.Collections.Generic;
using System.Text;

namespace Contract.Dtos
{
    public class TicketDepartmentStatisticsDto
    {
        public int DepartmentId { get; init; }

        public string DepartmentName { get; init; } = string.Empty;

        public int TicketCount { get; init; }

        public int OverdueCount { get; init; }

        public int UnassignedCount { get; init; }

        public double? AverageResolutionHours { get; init; }
    }

}
