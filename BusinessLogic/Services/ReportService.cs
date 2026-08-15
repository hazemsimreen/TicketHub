using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using Contract.Paged;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using TicketHub.DataAccess.Repositories;

namespace BusinessLogic.Services;

public class ReportService : IReportService
{
    private const int MaxDateRangeDays = 366;

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public ReportService(IUnitOfWork uow, ICurrentUser currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    /// <summary>Group by category, average stars, count — all done in SQL via GroupBy +
    /// Average + Count. No ticket/rating rows are ever loaded into memory.</summary>
    public async Task<ServiceResult<PagedResult<CategorySatisfactionDto>>> CategorySatisfactionAsync(
        PagedQuery query, CancellationToken ct = default)
    {
        var joined = _uow.Repository<Rating>().Query().AsNoTracking()
            .Join(
                _uow.Repository<Ticket>().Query().AsNoTracking(), // global !IsDeleted filter applies
                r => r.TicketId, t => t.Id, (r, t) => new { r, t });

        // Admin sees everything. Supervisor is department-scoped, same rule as
        // everywhere else a Supervisor touches ticket data.
        if (!_currentUser.IsInRole("Admin"))
        {
            if (_currentUser.PrimaryDepartmentId is null)
            {
                joined = joined.Where(_ => false);
            }
            else
            {
                joined = joined.Where(x => x.t.DepartmentId == _currentUser.PrimaryDepartmentId.Value);
            }
        }

        var grouped = joined
            .GroupBy(x => new { x.t.CategoryId, CategoryName = x.t.Category.Name })
            .Select(g => new CategorySatisfactionDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                AverageStars = g.Average(x => x.r.Stars),
                RatingCount = g.Count()
            });

        var totalCount = await grouped.CountAsync(ct);

        var items = await grouped
            .OrderByDescending(g => g.RatingCount)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var result = new PagedResult<CategorySatisfactionDto>(items, query.Page, query.PageSize, totalCount);

        return ServiceResult<PagedResult<CategorySatisfactionDto>>.Success(result);
    }

    /// <summary>Created vs resolved per day. Range is capped so nobody asks for years of daily buckets.</summary>
    public async Task<ServiceResult<List<DailyVolumeDto>>> DailyVolumeAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from)
        {
            return ServiceResult<List<DailyVolumeDto>>.BadRequest("'to' must not be before 'from'.");
        }

        if (to.DayNumber - from.DayNumber > MaxDateRangeDays)
        {
            return ServiceResult<List<DailyVolumeDto>>.BadRequest($"Range cannot exceed {MaxDateRangeDays} days.");
        }

        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        var tickets = _uow.Repository<Ticket>().Query().AsNoTracking();

        if (!_currentUser.IsInRole("Admin"))
        {
            if (_currentUser.PrimaryDepartmentId is null)
            {
                tickets = tickets.Where(_ => false);
            }
            else
            {
                tickets = tickets.Where(t => t.DepartmentId == _currentUser.PrimaryDepartmentId.Value);
            }
        }

        var created = await tickets
            .Where(t => t.CreatedAt >= fromDt && t.CreatedAt <= toDt)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var resolved = await tickets
            .Where(t => t.ResolvedAt != null && t.ResolvedAt >= fromDt && t.ResolvedAt <= toDt)
            .GroupBy(t => t.ResolvedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var days = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
            .Select(offset => from.AddDays(offset))
            .Select(d => new DailyVolumeDto
            {
                Date = d,
                CreatedCount = created.FirstOrDefault(x => DateOnly.FromDateTime(x.Date) == d)?.Count ?? 0,
                ResolvedCount = resolved.FirstOrDefault(x => DateOnly.FromDateTime(x.Date) == d)?.Count ?? 0
            })
            .ToList();

        return ServiceResult<List<DailyVolumeDto>>.Success(days);
    }

    /// <summary>Per agent: open tickets, tickets resolved this calendar month, and average
    /// resolution time in hours — computed server-side with EF.Functions.DateDiffHour so no
    /// per-ticket rows are pulled into memory just to subtract two dates.</summary>
    public async Task<ServiceResult<List<AgentWorkloadDto>>> AgentWorkloadAsync(CancellationToken ct = default)
    {
        var agents = _uow.Repository<Agent>().Query().AsNoTracking()
            .Where(a => !a.IsDeleted);

        if (!_currentUser.IsInRole("Admin"))
        {
            if (_currentUser.PrimaryDepartmentId is null)
            {
                agents = agents.Where(_ => false);
            }
            else
            {
                agents = agents.Where(a => a.DepartmentId == _currentUser.PrimaryDepartmentId.Value);
            }
        }

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Agents is a small table (one row per staff agent) — safe to materialize,
        // then run one aggregate query per agent. Each of those three queries is
        // still a single-value SQL aggregate (COUNT/COUNT/AVG), never a full
        // ticket fetch — "zero rows loaded" just applies per-query here instead
        // of in one mega-query, to keep the LINQ simple and reliable.
        var agentRows = await agents
            .Select(a => new
            {
                a.Id,
                a.UserId,
                UserName = a.User.UserName,
                a.DepartmentId
            })
            .ToListAsync(ct);

        var workload = new List<AgentWorkloadDto>(agentRows.Count);

        foreach (var a in agentRows)
        {
            var ticketsForAgent = _uow.Repository<Ticket>().Query().AsNoTracking()
                .Where(t => t.AssignedToUserId == a.UserId);

            var openCount = await ticketsForAgent
                .CountAsync(t => t.ResolvedAt == null, ct);

            var resolvedThisMonthCount = await ticketsForAgent
                .CountAsync(t => t.ResolvedAt != null && t.ResolvedAt >= monthStart, ct);

            var resolvedTickets = ticketsForAgent.Where(t => t.ResolvedAt != null);

            var hasResolved = await resolvedTickets.AnyAsync(ct);

            double? averageResolutionHours = hasResolved
                ? await resolvedTickets.AverageAsync(
                    t => (double)EF.Functions.DateDiffHour(t.CreatedAt, t.ResolvedAt!.Value), ct)
                : null;

            workload.Add(new AgentWorkloadDto
            {
                AgentId = a.Id,
                UserId = a.UserId,
                UserName = a.UserName ?? string.Empty,
                DepartmentId = a.DepartmentId,
                OpenCount = openCount,
                ResolvedThisMonthCount = resolvedThisMonthCount,
                AverageResolutionHours = averageResolutionHours
            });
        }

        workload = workload.OrderByDescending(a => a.OpenCount).ToList();

        return ServiceResult<List<AgentWorkloadDto>>.Success(workload);
    }
}
