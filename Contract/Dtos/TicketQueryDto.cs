using System;
using System.Collections.Generic;
using System.Text;
using Contract.Paged;

namespace Contract.Dtos
{
    public class TicketQueryDto : PagedQuery
    {
        public int? StatusId { get; set; }
        public int? PriorityId { get; set; }
        public int? CategoryId { get; set; }
        public int? DepartmentId { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public bool? Unassigned { get; set; }
        public bool? Overdue { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }
        public string? SortBy { get; set; }   // "createdAt_desc" (افتراضي), "createdAt_asc", "dueAt_asc", "dueAt_desc"
    }
}
