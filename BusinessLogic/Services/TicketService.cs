using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using Contract.Paged;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketHub.DataAccess.Repositories;

namespace BusinessLogic.Services;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;
    private readonly ITicketWorkflow _workflow;

    private const int MaxActiveTicketsPerAgent = 5;

    // ============================================================
    // Workflow Step Instance Status Codes
    //
    // ملاحظة: هذه ليست حالات التذكرة نفسها (Open/InProgress/...)
    // بل حالات "خطوة سير العمل" (WorkflowStepInstance) المرتبطة
    // بجداول WorkflowDefinition/WorkflowStep/WorkflowInstance.
    // ============================================================

    private const string WorkflowStepStatusInProgress = "InProgress";
    private const string WorkflowStepStatusCompleted = "Completed";
    private const string WorkflowStepStatusCancelled = "Cancelled";

    private const string WorkflowInstanceStatusInProgress = "InProgress";
    private const string WorkflowInstanceStatusCompleted = "Completed";
    private const string WorkflowInstanceStatusCancelled = "Cancelled";

    public TicketService(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        UserManager<User> userManager,
        ITicketWorkflow workflow)
    {
        _uow = uow;
        _currentUser = currentUser;
        _userManager = userManager;
        _workflow = workflow;
    }


    // ============================================================
    // CreateTicketAsync
    // ============================================================

    public async Task<ServiceResult<TicketDetailDto>> CreateTicketAsync(
        CreateTicketDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return ServiceResult<TicketDetailDto>.BadRequest(
                "Title is required.");
        }

        if (_currentUser.UserId is null ||
            !Guid.TryParse(
                _currentUser.UserId,
                out var submittedByUserId))
        {
            return ServiceResult<TicketDetailDto>.Unauthorized(
                "User is not authenticated.");
        }

        var category =
            await _uow.Repository<Category>()
                .Query()
                .Include(c => c.Department)
                .FirstOrDefaultAsync(
                    c => c.Id == dto.CategoryId,
                    cancellationToken);

        if (category is null)
        {
            return ServiceResult<TicketDetailDto>.NotFound(
                "Category not found.");
        }

        if (category.DefaultPriorityId is null)
        {
            return ServiceResult<TicketDetailDto>.BadRequest(
                "Category has no default priority configured.");
        }

        var openStatus =
            await _uow.Repository<TicketStatus>()
                .Query()
                .FirstOrDefaultAsync(
                    s => s.Code == "Open",
                    cancellationToken);

        if (openStatus is null)
        {
            return ServiceResult<TicketDetailDto>.BadRequest(
                "Ticket status 'Open' is not configured.");
        }

        var priority =
            await _uow.Repository<TicketPriority>()
                .GetByIdAsync(
                    category.DefaultPriorityId.Value,
                    cancellationToken);

        if (priority is null)
        {
            return ServiceResult<TicketDetailDto>.BadRequest(
                "Default priority for this category is not configured correctly.");
        }

        var submittedByUser =
            await _userManager.FindByIdAsync(
                submittedByUserId.ToString());

        if (submittedByUser is null)
        {
            return ServiceResult<TicketDetailDto>.Unauthorized(
                "User is not authenticated.");
        }

        var ticketNumber =
            GenerateTicketNumber();

        var dueAt =
            DateTime.UtcNow.Add(
                GetSlaDuration(priority.Code));

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketNumber = ticketNumber,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            SubmittedByUserId = submittedByUserId,
            CategoryId = category.Id,
            DepartmentId = category.DepartmentId,
            PriorityId = priority.Id,
            StatusId = openStatus.Id,
            DueAt = dueAt,
            CreatedBy = submittedByUserId.ToString()
        };

        var statusHistory =
            new TicketStatusHistory
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                FromStatusId = null,
                ToStatusId = openStatus.Id,
                ChangedByUserId = submittedByUserId
            };

        await _uow.Repository<Ticket>()
            .AddAsync(
                ticket,
                cancellationToken);

        await _uow.Repository<TicketStatusHistory>()
            .AddAsync(
                statusHistory,
                cancellationToken);


        // --------------------------------------------------------
        // Workflow Engine
        //
        // ننشئ WorkflowInstance تلقائياً حسب القالب المناسب
        // (Department + Category، أو القالب الافتراضي للقسم)،
        // ونبدأ أول خطوة (StepOrder = 1).
        //
        // لو ما في قالب مهيأ لهذا القسم/الفئة، نتجاهل الأمر بأمان
        // (الـ Workflow ميزة اختيارية ولا يجب أن توقف إنشاء التذكرة).
        // --------------------------------------------------------

        await CreateWorkflowInstanceAsync(
            ticket,
            cancellationToken);


        await _uow.SaveChangesAsync(
            cancellationToken);

        var resultDto =
            new TicketDetailDto
            {
                Id = ticket.Id,

                TicketNumber =
                    ticket.TicketNumber,

                Title =
                    ticket.Title,

                Description =
                    ticket.Description,

                StatusCode =
                    openStatus.Code,

                PriorityCode =
                    priority.Code,

                CategoryName =
                    category.Name,

                DepartmentName =
                    category.Department.Name,

                SubmittedByName =
                    submittedByUser.UserName ??
                    submittedByUser.Email ??
                    "Unknown",

                AssignedToName =
                    null,

                CreatedAt =
                    ticket.CreatedAt,

                DueAt =
                    ticket.DueAt,

                IsOverdue =
                    false,

                RowVersion =
                    Convert.ToBase64String(
                        ticket.RowVersion)
            };

        return ServiceResult<TicketDetailDto>
            .Success(resultDto);
    }


    // ============================================================
    // GenerateTicketNumber
    // ============================================================

    private static string GenerateTicketNumber()
    {
        var datePart =
            DateTime.UtcNow.ToString("yyyyMMdd");

        var randomPart =
            Guid.NewGuid()
                .ToString("N")[..6]
                .ToUpperInvariant();

        return $"TKT-{datePart}-{randomPart}";
    }


    // ============================================================
    // SLA
    // ============================================================

    private static TimeSpan GetSlaDuration(
        string priorityCode)
    {
        return priorityCode switch
        {
            "Urgent" =>
                TimeSpan.FromHours(4),

            "High" =>
                TimeSpan.FromHours(24),

            "Medium" =>
                TimeSpan.FromHours(72),

            "Low" =>
                TimeSpan.FromHours(168),

            _ =>
                TimeSpan.FromHours(72)
        };
    }


    // ============================================================
    // GetTicketByIdAsync
    // ============================================================

    public async Task<ServiceResult<TicketDetailDto>>
        GetTicketByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        IQueryable<Ticket> query =
            _uow.Repository<Ticket>()
                .Query()
                .AsNoTracking()
                .Include(t => t.Category)
                .Include(t => t.Department)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .Include(t => t.SubmittedByUser)
                .Include(t => t.AssignedToUser);

        query =
            ApplyAccessFilter(query);

        var ticket =
            await query.FirstOrDefaultAsync(
                t => t.Id == id,
                cancellationToken);

        if (ticket is null)
        {
            return ServiceResult<TicketDetailDto>
                .NotFound(
                    "Ticket not found.");
        }

        var dto =
            BuildDetailDto(ticket);

        return ServiceResult<TicketDetailDto>
            .Success(dto);
    }


    // ============================================================
    // ApplyAccessFilter
    // ============================================================

    private IQueryable<Ticket> ApplyAccessFilter(
        IQueryable<Ticket> query)
    {
        // ========================================================
        // Admin
        // ========================================================

        if (_currentUser.IsInRole("Admin"))
        {
            return query;
        }


        // ========================================================
        // DepartmentHead
        // ========================================================

        if (_currentUser.IsInRole("DepartmentHead"))
        {
            if (_currentUser.PrimaryDepartmentId is null)
            {
                return query.Where(_ => false);
            }

            return query.Where(t =>
                t.DepartmentId ==
                _currentUser.PrimaryDepartmentId.Value);
        }


        // ========================================================
        // Employee
        // ========================================================

        if (_currentUser.IsInRole("Employee"))
        {
            if (_currentUser.UserId is null ||
                !Guid.TryParse(
                    _currentUser.UserId,
                    out var employeeId))
            {
                return query.Where(_ => false);
            }

            return query.Where(t =>
                t.AssignedToUserId ==
                employeeId);
        }


        // ========================================================
        // Citizen
        // ========================================================

        if (_currentUser.IsInRole("Citizen"))
        {
            if (_currentUser.UserId is null ||
                !Guid.TryParse(
                    _currentUser.UserId,
                    out var citizenId))
            {
                return query.Where(_ => false);
            }

            return query.Where(t =>
                t.SubmittedByUserId ==
                citizenId);
        }


        // ========================================================
        // Unknown Role
        // Fail Closed
        // ========================================================

        return query.Where(_ => false);
    }


    // ============================================================
    // ListTicketsAsync
    // ============================================================

    public async Task<
        ServiceResult<PagedResult<TicketListItemDto>>>
        ListTicketsAsync(
            TicketQueryDto queryDto,
            CancellationToken cancellationToken = default)
    {
        IQueryable<Ticket> query =
            _uow.Repository<Ticket>()
                .Query()
                .AsNoTracking();

        query =
            ApplyAccessFilter(query);


        if (queryDto.StatusId.HasValue)
        {
            query = query.Where(t =>
                t.StatusId ==
                queryDto.StatusId.Value);
        }


        if (queryDto.PriorityId.HasValue)
        {
            query = query.Where(t =>
                t.PriorityId ==
                queryDto.PriorityId.Value);
        }


        if (queryDto.CategoryId.HasValue)
        {
            query = query.Where(t =>
                t.CategoryId ==
                queryDto.CategoryId.Value);
        }


        if (queryDto.DepartmentId.HasValue)
        {
            query = query.Where(t =>
                t.DepartmentId ==
                queryDto.DepartmentId.Value);
        }


        if (queryDto.AssignedToUserId.HasValue)
        {
            query = query.Where(t =>
                t.AssignedToUserId ==
                queryDto.AssignedToUserId.Value);
        }


        if (queryDto.Unassigned == true)
        {
            query = query.Where(t =>
                t.AssignedToUserId == null);
        }


        if (queryDto.Overdue == true)
        {
            var now =
                DateTime.UtcNow;

            query = query.Where(t =>
                t.DueAt.HasValue &&
                t.DueAt.Value < now &&
                t.Status.Code != "Resolved" &&
                t.Status.Code != "Closed" &&
                t.Status.Code != "Cancelled");
        }


        if (queryDto.FromDate.HasValue)
        {
            query = query.Where(t =>
                t.CreatedAt >=
                queryDto.FromDate.Value);
        }


        if (queryDto.ToDate.HasValue)
        {
            query = query.Where(t =>
                t.CreatedAt <=
                queryDto.ToDate.Value);
        }


        if (!string.IsNullOrWhiteSpace(
            queryDto.Search))
        {
            var term =
                queryDto.Search.Trim();

            query = query.Where(t =>
                t.Title.Contains(term) ||
                t.TicketNumber.Contains(term));
        }


        var total =
            await query.CountAsync(
                cancellationToken);


        query =
            queryDto.SortBy switch
            {
                "createdAt_asc" =>
                    query
                        .OrderBy(t => t.CreatedAt)
                        .ThenBy(t => t.Id),

                "dueAt_asc" =>
                    query
                        .OrderBy(t => t.DueAt)
                        .ThenBy(t => t.Id),

                "dueAt_desc" =>
                    query
                        .OrderByDescending(t => t.DueAt)
                        .ThenByDescending(t => t.Id),

                _ =>
                    query
                        .OrderByDescending(
                            t => t.CreatedAt)
                        .ThenByDescending(
                            t => t.Id)
            };


        var items =
            await query
                .Skip(
                    (queryDto.Page - 1) *
                    queryDto.PageSize)
                .Take(queryDto.PageSize)
                .Select(t =>
                    new TicketListItemDto
                    {
                        Id =
                            t.Id,

                        TicketNumber =
                            t.TicketNumber,

                        Title =
                            t.Title,

                        StatusCode =
                            t.Status.Code,

                        PriorityCode =
                            t.Priority.Code,

                        CategoryName =
                            t.Category.Name,

                        DepartmentName =
                            t.Department.Name,

                        SubmittedByName =
                            t.SubmittedByUser.UserName ??
                            t.SubmittedByUser.Email ??
                            "Unknown",

                        AssignedToName =
                            t.AssignedToUser != null
                                ? (
                                    t.AssignedToUser.UserName ??
                                    t.AssignedToUser.Email
                                  )
                                : null,

                        CreatedAt =
                            t.CreatedAt,

                        DueAt =
                            t.DueAt,

                        IsOverdue =
                            t.DueAt.HasValue &&
                            t.DueAt.Value < DateTime.UtcNow &&
                            t.Status.Code != "Resolved" &&
                            t.Status.Code != "Closed" &&
                            t.Status.Code != "Cancelled"
                    })
                .ToListAsync(
                    cancellationToken);


        var pagedResult =
            new PagedResult<TicketListItemDto>(
                items,
                queryDto.Page,
                queryDto.PageSize,
                total);


        return ServiceResult<
            PagedResult<TicketListItemDto>>
            .Success(pagedResult);
    }


    // ============================================================
    // GetMyTicketsAsync
    // ============================================================

    public async Task<
        ServiceResult<PagedResult<TicketListItemDto>>>
        GetMyTicketsAsync(
            TicketQueryDto queryDto,
            CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null ||
            !Guid.TryParse(
                _currentUser.UserId,
                out var userId))
        {
            return ServiceResult<
                PagedResult<TicketListItemDto>>
                .Unauthorized(
                    "User is not authenticated.");
        }


        IQueryable<Ticket> query =
            _uow.Repository<Ticket>()
                .Query()
                .AsNoTracking();


        var isEmployee =
            _currentUser.IsInRole("Employee");


        if (isEmployee)
        {
            query =
                query.Where(t =>
                    t.SubmittedByUserId == userId ||
                    t.AssignedToUserId == userId);
        }
        else
        {
            query =
                query.Where(t =>
                    t.SubmittedByUserId == userId);
        }


        if (queryDto.StatusId.HasValue)
        {
            query =
                query.Where(t =>
                    t.StatusId ==
                    queryDto.StatusId.Value);
        }


        if (queryDto.PriorityId.HasValue)
        {
            query =
                query.Where(t =>
                    t.PriorityId ==
                    queryDto.PriorityId.Value);
        }


        if (queryDto.CategoryId.HasValue)
        {
            query =
                query.Where(t =>
                    t.CategoryId ==
                    queryDto.CategoryId.Value);
        }


        if (queryDto.DepartmentId.HasValue)
        {
            query =
                query.Where(t =>
                    t.DepartmentId ==
                    queryDto.DepartmentId.Value);
        }


        if (queryDto.Overdue == true)
        {
            var now =
                DateTime.UtcNow;

            query =
                query.Where(t =>
                    t.DueAt.HasValue &&
                    t.DueAt.Value < now &&
                    t.Status.Code != "Resolved" &&
                    t.Status.Code != "Closed" &&
                    t.Status.Code != "Cancelled");
        }


        if (queryDto.FromDate.HasValue)
        {
            query =
                query.Where(t =>
                    t.CreatedAt >=
                    queryDto.FromDate.Value);
        }


        if (queryDto.ToDate.HasValue)
        {
            query =
                query.Where(t =>
                    t.CreatedAt <=
                    queryDto.ToDate.Value);
        }


        if (!string.IsNullOrWhiteSpace(
            queryDto.Search))
        {
            var term =
                queryDto.Search.Trim();

            query =
                query.Where(t =>
                    t.Title.Contains(term) ||
                    t.TicketNumber.Contains(term));
        }


        var total =
            await query.CountAsync(
                cancellationToken);


        query =
            queryDto.SortBy switch
            {
                "createdAt_asc" =>
                    query
                        .OrderBy(t => t.CreatedAt)
                        .ThenBy(t => t.Id),

                "dueAt_asc" =>
                    query
                        .OrderBy(t => t.DueAt)
                        .ThenBy(t => t.Id),

                "dueAt_desc" =>
                    query
                        .OrderByDescending(t => t.DueAt)
                        .ThenByDescending(t => t.Id),

                _ =>
                    query
                        .OrderByDescending(
                            t => t.CreatedAt)
                        .ThenByDescending(
                            t => t.Id)
            };


        var items =
            await query
                .Skip(
                    (queryDto.Page - 1) *
                    queryDto.PageSize)
                .Take(queryDto.PageSize)
                .Select(t =>
                    new TicketListItemDto
                    {
                        Id =
                            t.Id,

                        TicketNumber =
                            t.TicketNumber,

                        Title =
                            t.Title,

                        StatusCode =
                            t.Status.Code,

                        PriorityCode =
                            t.Priority.Code,

                        CategoryName =
                            t.Category.Name,

                        DepartmentName =
                            t.Department.Name,

                        SubmittedByName =
                            t.SubmittedByUser.UserName ??
                            t.SubmittedByUser.Email ??
                            "Unknown",

                        AssignedToName =
                            t.AssignedToUser != null
                                ? (
                                    t.AssignedToUser.UserName ??
                                    t.AssignedToUser.Email
                                  )
                                : null,

                        CreatedAt =
                            t.CreatedAt,

                        DueAt =
                            t.DueAt,

                        IsOverdue =
                            t.DueAt.HasValue &&
                            t.DueAt.Value < DateTime.UtcNow &&
                            t.Status.Code != "Resolved" &&
                            t.Status.Code != "Closed" &&
                            t.Status.Code != "Cancelled"
                    })
                .ToListAsync(
                    cancellationToken);


        var pagedResult =
            new PagedResult<TicketListItemDto>(
                items,
                queryDto.Page,
                queryDto.PageSize,
                total);


        return ServiceResult<
            PagedResult<TicketListItemDto>>
            .Success(pagedResult);
    }


    // ============================================================
    // UpdateTicketAsync
    // ============================================================

    public async Task<ServiceResult<TicketDetailDto>>
    UpdateTicketAsync(
        Guid id,
        UpdateTicketDto dto,
        CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null ||
            !Guid.TryParse(
                _currentUser.UserId,
                out var currentUserId))
        {
            return ServiceResult<TicketDetailDto>
                .Unauthorized(
                    "User is not authenticated.");
        }


        byte[] rowVersionBytes;

        try
        {
            rowVersionBytes =
                Convert.FromBase64String(
                    dto.RowVersion);
        }
        catch (FormatException)
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    "Invalid RowVersion format.");
        }


        IQueryable<Ticket> query =
            _uow.Repository<Ticket>()
                .Query()
                .Include(t => t.Category)
                .Include(t => t.Department)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .Include(t => t.SubmittedByUser)
                .Include(t => t.AssignedToUser);


        query =
            ApplyAccessFilter(query);


        var ticket =
            await query.FirstOrDefaultAsync(
                t => t.Id == id,
                cancellationToken);


        if (ticket is null)
        {
            return ServiceResult<TicketDetailDto>
                .NotFound(
                    "Ticket not found.");
        }


        var category =
            await _uow.Repository<Category>()
                .Query()
                .Include(c => c.Department)
                .FirstOrDefaultAsync(
                    c => c.Id == dto.CategoryId,
                    cancellationToken);


        if (category is null)
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    "Category not found.");
        }


        var priority =
            await _uow.Repository<TicketPriority>()
                .GetByIdAsync(
                    dto.PriorityId,
                    cancellationToken);


        if (priority is null)
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    "Priority not found.");
        }


        // --------------------------------------------------------
        // تغيير الأولوية مسموح فقط لـ Admin أو DepartmentHead
        // --------------------------------------------------------

        var isPriorityChanged =
            ticket.PriorityId != priority.Id;

        if (isPriorityChanged)
        {
            var canChangePriority =
                _currentUser.IsInRole("Admin") ||
                _currentUser.IsInRole("DepartmentHead");

            if (!canChangePriority)
            {
                return ServiceResult<TicketDetailDto>
                    .Forbidden(
                        "Only Admin or DepartmentHead can change ticket priority.");
            }
        }


        ticket.Title =
            dto.Title.Trim();

        ticket.Description =
            dto.Description.Trim();

        ticket.CategoryId =
            category.Id;

        ticket.DepartmentId =
            category.DepartmentId;

        ticket.PriorityId =
            priority.Id;

        ticket.UpdatedAt =
            DateTime.UtcNow;

        ticket.UpdatedBy =
            currentUserId.ToString();


        _uow.SetOriginalValue(
            ticket,
            t => t.RowVersion,
            rowVersionBytes);


        try
        {
            await _uow.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<TicketDetailDto>
                .Conflict(
                    "This ticket was modified by someone else. Please reload and try again.");
        }


        return ServiceResult<TicketDetailDto>
            .Success(
                BuildDetailDto(ticket));
    }

    // ============================================================
    // UpdateTicketStatusAsync
    // ============================================================

    public async Task<ServiceResult<TicketDetailDto>>
        UpdateTicketStatusAsync(
            Guid id,
            UpdateTicketStatusDto dto,
            CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null ||
            !Guid.TryParse(
                _currentUser.UserId,
                out var currentUserId))
        {
            return ServiceResult<TicketDetailDto>
                .Unauthorized(
                    "User is not authenticated.");
        }


        if (string.IsNullOrWhiteSpace(
            dto.NewStatusCode))
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    "NewStatusCode is required.");
        }


        IQueryable<Ticket> query =
            _uow.Repository<Ticket>()
                .Query()
                .Include(t => t.Category)
                .Include(t => t.Department)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .Include(t => t.SubmittedByUser)
                .Include(t => t.AssignedToUser);


        query =
            ApplyAccessFilter(query);


        var ticket =
            await query.FirstOrDefaultAsync(
                t => t.Id == id,
                cancellationToken);


        if (ticket is null)
        {
            return ServiceResult<TicketDetailDto>
                .NotFound(
                    "Ticket not found.");
        }


        var newStatus =
            await _uow.Repository<TicketStatus>()
                .Query()
                .FirstOrDefaultAsync(
                    s => s.Code ==
                         dto.NewStatusCode,
                    cancellationToken);


        if (newStatus is null)
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    $"Status '{dto.NewStatusCode}' is not configured.");
        }


        var currentStatusCode =
            ticket.Status.Code;


        if (newStatus.Code == "Cancelled" &&
            string.IsNullOrWhiteSpace(
                dto.Reason))
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    "A reason is required when cancelling a ticket.");
        }


        if (!_workflow.CanTransition(
                currentStatusCode,
                newStatus.Code))
        {
            var allowed =
                _workflow.GetAllowedTransitions(
                    currentStatusCode);

            var allowedText =
                allowed.Count > 0
                    ? string.Join(", ", allowed)
                    : "none (terminal status)";

            return ServiceResult<TicketDetailDto>
                .Conflict(
                    $"Cannot transition from '{currentStatusCode}' to '{newStatus.Code}'. Allowed transitions: {allowedText}.");
        }


        var oldStatusId =
            ticket.StatusId;


        if (newStatus.Code == "Resolved")
        {
            ticket.ResolvedAt =
                DateTime.UtcNow;
        }
        else if (
            ticket.ResolvedAt.HasValue &&
            newStatus.Code != "Closed")
        {
            ticket.ResolvedAt =
                null;
        }


        ticket.StatusId =
            newStatus.Id;

        ticket.UpdatedAt =
            DateTime.UtcNow;

        ticket.UpdatedBy =
            currentUserId.ToString();


        var statusHistory =
            new TicketStatusHistory
            {
                Id =
                    Guid.NewGuid(),

                TicketId =
                    ticket.Id,

                FromStatusId =
                    oldStatusId,

                ToStatusId =
                    newStatus.Id,

                ChangedByUserId =
                    currentUserId
            };


        await _uow.Repository<TicketStatusHistory>()
            .AddAsync(
                statusHistory,
                cancellationToken);


        // --------------------------------------------------------
        // Workflow Engine
        //
        // نُقدّم/نُقفل خطوة سير العمل الحالية بناءً على الحالة
        // الجديدة للتذكرة. هذا منفصل تماماً عن قواعد الانتقال
        // (Open/InProgress/...) التي تبقى مضبوطة عبر ITicketWorkflow.
        // --------------------------------------------------------

        await AdvanceWorkflowInstanceAsync(
            ticket.Id,
            newStatus.Code,
            cancellationToken);


        var notificationType =
            await _uow.Repository<NotificationType>()
                .Query()
                .FirstOrDefaultAsync(
                    nt =>
                        nt.Code ==
                        "TicketStatusChanged",
                    cancellationToken);


        if (notificationType is not null)
        {
            var notification =
                new Notification
                {
                    Id =
                        Guid.NewGuid(),

                    RecipientUserId =
                        ticket.SubmittedByUserId,

                    NotificationTypeId =
                        notificationType.Id,

                    TicketId =
                        ticket.Id,

                    IsRead =
                        false
                };


            await _uow.Repository<Notification>()
                .AddAsync(
                    notification,
                    cancellationToken);
        }


        await _uow.SaveChangesAsync(
            cancellationToken);


        ticket.Status =
            newStatus;


        return ServiceResult<TicketDetailDto>
            .Success(
                BuildDetailDto(ticket));
    }


    // ============================================================
    // AssignTicketAsync
    // ============================================================

    public async Task<ServiceResult<TicketDetailDto>>
        AssignTicketAsync(
            Guid id,
            AssignTicketDto dto,
            CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null ||
            !Guid.TryParse(
                _currentUser.UserId,
                out var currentUserId))
        {
            return ServiceResult<TicketDetailDto>
                .Unauthorized(
                    "User is not authenticated.");
        }


        IQueryable<Ticket> query =
            _uow.Repository<Ticket>()
                .Query()
                .Include(t => t.Category)
                .Include(t => t.Department)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .Include(t => t.SubmittedByUser)
                .Include(t => t.AssignedToUser);


        query =
            ApplyAccessFilter(query);


        var ticket =
            await query.FirstOrDefaultAsync(
                t => t.Id == id,
                cancellationToken);


        if (ticket is null)
        {
            return ServiceResult<TicketDetailDto>
                .NotFound(
                    "Ticket not found.");
        }


        // --------------------------------------------------------
        // Unassign
        // --------------------------------------------------------

        if (dto.AssignedToUserId is null)
        {
            ticket.AssignedToUserId =
                null;

            ticket.UpdatedAt =
                DateTime.UtcNow;

            ticket.UpdatedBy =
                currentUserId.ToString();


            // ----------------------------------------------------
            // Workflow Engine
            // نزيل موظف التنفيذ عن خطوة سير العمل الحالية أيضاً
            // ----------------------------------------------------

            await SyncWorkflowStepAssigneeAsync(
                ticket.Id,
                null,
                cancellationToken);


            await _uow.SaveChangesAsync(
                cancellationToken);


            return ServiceResult<TicketDetailDto>
                .Success(
                    BuildDetailDto(ticket));
        }


        var assignee =
            await _userManager.FindByIdAsync(
                dto.AssignedToUserId.Value.ToString());


        if (assignee is null)
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    "Assigned user not found.");
        }


        if (assignee.PrimaryDepartmentId !=
            ticket.DepartmentId)
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    "Assigned user must belong to the same department as the ticket.");
        }


        var isEmployee =
            await _uow.Repository<UserRole>()
                .Query()
                .Include(ur => ur.Role)
                .AnyAsync(
                    ur =>
                        ur.UserId ==
                        assignee.Id &&
                        ur.Role.Code ==
                        "Employee",
                    cancellationToken);


        if (!isEmployee)
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    "Assigned user must have the Employee role.");
        }


        var activeTicketsCount =
            await _uow.Repository<Ticket>()
                .Query()
                .Where(t =>
                    t.AssignedToUserId ==
                    assignee.Id &&

                    t.Status.Code !=
                    "Resolved" &&

                    t.Status.Code !=
                    "Closed" &&

                    t.Status.Code !=
                    "Cancelled")
                .CountAsync(
                    cancellationToken);


        if (activeTicketsCount >=
            MaxActiveTicketsPerAgent)
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    $"Assigned user already has {activeTicketsCount} active tickets (max {MaxActiveTicketsPerAgent}).");
        }


        ticket.AssignedToUserId =
            assignee.Id;

        ticket.UpdatedAt =
            DateTime.UtcNow;

        ticket.UpdatedBy =
            currentUserId.ToString();


        // --------------------------------------------------------
        // Workflow Engine
        // نزامن موظف التنفيذ مع خطوة سير العمل الحالية (إن وجدت)
        // --------------------------------------------------------

        await SyncWorkflowStepAssigneeAsync(
            ticket.Id,
            assignee.Id,
            cancellationToken);


        await _uow.SaveChangesAsync(
            cancellationToken);


        ticket.AssignedToUser =
            assignee;


        return ServiceResult<TicketDetailDto>
            .Success(
                BuildDetailDto(ticket));
    }


    // ============================================================
    // AutoAssignTicketAsync
    // ============================================================

    public async Task<ServiceResult<TicketDetailDto>>
        AutoAssignTicketAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null ||
            !Guid.TryParse(
                _currentUser.UserId,
                out var currentUserId))
        {
            return ServiceResult<TicketDetailDto>
                .Unauthorized(
                    "User is not authenticated.");
        }


        IQueryable<Ticket> query =
            _uow.Repository<Ticket>()
                .Query()
                .Include(t => t.Category)
                .Include(t => t.Department)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .Include(t => t.SubmittedByUser)
                .Include(t => t.AssignedToUser);


        query =
            ApplyAccessFilter(query);


        var ticket =
            await query.FirstOrDefaultAsync(
                t => t.Id == id,
                cancellationToken);


        if (ticket is null)
        {
            return ServiceResult<TicketDetailDto>
                .NotFound(
                    "Ticket not found.");
        }


        var leastLoadedAgent =
            await _userManager.Users
                .Where(u =>
                    u.PrimaryDepartmentId ==
                    ticket.DepartmentId &&

                    u.UserRoles.Any(
                        ur =>
                            ur.Role.Code ==
                            "Employee"))
                .Select(u =>
                    new
                    {
                        User = u,

                        ActiveTicketsCount =
                            u.AssignedTickets.Count(
                                t =>
                                    t.Status.Code !=
                                    "Resolved" &&

                                    t.Status.Code !=
                                    "Closed" &&

                                    t.Status.Code !=
                                    "Cancelled")
                    })
                .OrderBy(
                    x =>
                        x.ActiveTicketsCount)
                .FirstOrDefaultAsync(
                    cancellationToken);


        if (leastLoadedAgent is null)
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    "No employees available in this department.");
        }


        if (leastLoadedAgent.ActiveTicketsCount >=
            MaxActiveTicketsPerAgent)
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    $"All employees in this department are at full capacity (max {MaxActiveTicketsPerAgent} active tickets each).");
        }


        ticket.AssignedToUserId =
            leastLoadedAgent.User.Id;

        ticket.UpdatedAt =
            DateTime.UtcNow;

        ticket.UpdatedBy =
            currentUserId.ToString();


        // --------------------------------------------------------
        // Workflow Engine
        // نزامن موظف التنفيذ مع خطوة سير العمل الحالية (إن وجدت)
        // --------------------------------------------------------

        await SyncWorkflowStepAssigneeAsync(
            ticket.Id,
            leastLoadedAgent.User.Id,
            cancellationToken);


        await _uow.SaveChangesAsync(
            cancellationToken);


        ticket.AssignedToUser =
            leastLoadedAgent.User;


        return ServiceResult<TicketDetailDto>
            .Success(
                BuildDetailDto(ticket));
    }


    // ============================================================
    // ReopenTicketAsync
    // ============================================================

    public async Task<ServiceResult<TicketDetailDto>>
        ReopenTicketAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null ||
            !Guid.TryParse(
                _currentUser.UserId,
                out var currentUserId))
        {
            return ServiceResult<TicketDetailDto>
                .Unauthorized(
                    "User is not authenticated.");
        }


        IQueryable<Ticket> query =
            _uow.Repository<Ticket>()
                .Query()
                .Include(t => t.Category)
                .Include(t => t.Department)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .Include(t => t.SubmittedByUser)
                .Include(t => t.AssignedToUser);


        query =
            ApplyAccessFilter(query);


        var ticket =
            await query.FirstOrDefaultAsync(
                t => t.Id == id,
                cancellationToken);


        if (ticket is null)
        {
            return ServiceResult<TicketDetailDto>
                .NotFound(
                    "Ticket not found.");
        }


        if (ticket.Status.Code != "Resolved")
        {
            return ServiceResult<TicketDetailDto>
                .Conflict(
                    $"Only resolved tickets can be reopened. Current status is '{ticket.Status.Code}'.");
        }


        var inProgressStatus =
            await _uow.Repository<TicketStatus>()
                .Query()
                .FirstOrDefaultAsync(
                    s =>
                        s.Code ==
                        "InProgress",
                    cancellationToken);


        if (inProgressStatus is null)
        {
            return ServiceResult<TicketDetailDto>
                .BadRequest(
                    "Ticket status 'InProgress' is not configured.");
        }


        if (!_workflow.CanTransition(
                ticket.Status.Code,
                inProgressStatus.Code))
        {
            return ServiceResult<TicketDetailDto>
                .Conflict(
                    $"Cannot transition from '{ticket.Status.Code}' to '{inProgressStatus.Code}'.");
        }


        var oldStatusId =
            ticket.StatusId;


        ticket.StatusId =
            inProgressStatus.Id;

        ticket.ResolvedAt =
            null;

        ticket.UpdatedAt =
            DateTime.UtcNow;

        ticket.UpdatedBy =
            currentUserId.ToString();


        var statusHistory =
            new TicketStatusHistory
            {
                Id =
                    Guid.NewGuid(),

                TicketId =
                    ticket.Id,

                FromStatusId =
                    oldStatusId,

                ToStatusId =
                    inProgressStatus.Id,

                ChangedByUserId =
                    currentUserId
            };


        await _uow.Repository<TicketStatusHistory>()
            .AddAsync(
                statusHistory,
                cancellationToken);


        // --------------------------------------------------------
        // Workflow Engine
        // نعيد فتح آخر خطوة كانت مكتملة/مقفلة على الـ WorkflowInstance
        // المرتبط بهذه التذكرة (إن وجد)
        // --------------------------------------------------------

        await ReopenWorkflowInstanceAsync(
            ticket.Id,
            cancellationToken);


        await _uow.SaveChangesAsync(
            cancellationToken);


        ticket.Status =
            inProgressStatus;


        return ServiceResult<TicketDetailDto>
            .Success(
                BuildDetailDto(ticket));
    }


    // ============================================================
    // GetTicketHistoryAsync
    // ============================================================

    public async Task<
        ServiceResult<PagedResult<TicketHistoryDto>>>
        GetTicketHistoryAsync(
            Guid ticketId,
            PagedQuery query,
            CancellationToken cancellationToken = default)
    {
        IQueryable<Ticket> ticketQuery =
            _uow.Repository<Ticket>()
                .Query()
                .AsNoTracking();


        ticketQuery =
            ApplyAccessFilter(ticketQuery);


        var ticketExists =
            await ticketQuery.AnyAsync(
                t => t.Id == ticketId,
                cancellationToken);


        if (!ticketExists)
        {
            return ServiceResult<
                PagedResult<TicketHistoryDto>>
                .NotFound(
                    "Ticket not found.");
        }


        var statusHistoryQuery =
            _uow.Repository<TicketStatusHistory>()
                .Query()
                .AsNoTracking()
                .Where(h =>
                    h.TicketId ==
                    ticketId)
                .Select(h =>
                    new TicketHistoryDto
                    {
                        Id =
                            h.Id,

                        Type =
                            "StatusChanged",

                        FieldName =
                            null,

                        FromStatusCode =
                            h.FromStatus != null
                                ? h.FromStatus.Code
                                : null,

                        ToStatusCode =
                            h.ToStatus.Code,

                        ChangedByName =
                            h.ChangedByUser.UserName ??
                            h.ChangedByUser.Email ??
                            "Unknown",

                        ChangedAt =
                            h.CreatedAt
                    });


        var fieldHistoryQuery =
            _uow.Repository<TicketFieldHistory>()
                .Query()
                .AsNoTracking()
                .Where(h =>
                    h.TicketId ==
                    ticketId)
                .Select(h =>
                    new TicketHistoryDto
                    {
                        Id =
                            h.Id,

                        Type =
                            "FieldChanged",

                        FieldName =
                            h.FieldName,

                        FromStatusCode =
                            null,

                        ToStatusCode =
                            null,

                        ChangedByName =
                            h.ChangedByUser.UserName ??
                            h.ChangedByUser.Email ??
                            "Unknown",

                        ChangedAt =
                            h.CreatedAt
                    });


        var historyQuery =
            statusHistoryQuery
                .Concat(
                    fieldHistoryQuery);


        var totalCount =
            await historyQuery.CountAsync(
                cancellationToken);


        historyQuery =
            historyQuery
                .OrderByDescending(
                    h => h.ChangedAt)
                .ThenByDescending(
                    h => h.Id);


        var items =
            await historyQuery
                .Skip(
                    (query.Page - 1) *
                    query.PageSize)
                .Take(
                    query.PageSize)
                .ToListAsync(
                    cancellationToken);


        var pagedResult =
            new PagedResult<TicketHistoryDto>(
                items,
                query.Page,
                query.PageSize,
                totalCount);


        return ServiceResult<
            PagedResult<TicketHistoryDto>>
            .Success(
                pagedResult);
    }


    // ============================================================
    // DeleteTicketAsync
    // ============================================================

    public async Task<ServiceResult>
        DeleteTicketAsync(
            Guid id,
            CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ServiceResult.Unauthorized(
                "User is not authenticated.");
        }


        if (!_currentUser.IsInRole("Admin"))
        {
            return ServiceResult.Forbidden(
                "Only Admin can delete tickets.");
        }


        if (_currentUser.UserId is null ||
            !Guid.TryParse(
                _currentUser.UserId,
                out var currentUserId))
        {
            return ServiceResult.Unauthorized(
                "User is not authenticated.");
        }


        var ticket =
            await _uow.Repository<Ticket>()
                .Query()
                .FirstOrDefaultAsync(
                    t => t.Id == id,
                    cancellationToken);


        if (ticket is null)
        {
            return ServiceResult.NotFound(
                "Ticket not found.");
        }


        ticket.IsDeleted =
            true;

        ticket.DeletedAt =
            DateTime.UtcNow;

        ticket.DeletedBy =
            currentUserId.ToString();

        ticket.UpdatedAt =
            DateTime.UtcNow;

        ticket.UpdatedBy =
            currentUserId.ToString();


        await _uow.SaveChangesAsync(
            cancellationToken);


        return ServiceResult.NoContent();
    }


    // ============================================================
    // GetTicketStatisticsAsync
    // ============================================================

    public async Task<
        ServiceResult<TicketStatisticsDto>>
        GetTicketStatisticsAsync(
            CancellationToken cancellationToken = default)
    {
        // ========================================================
        // Authentication
        // ========================================================

        if (!_currentUser.IsAuthenticated)
        {
            return ServiceResult<TicketStatisticsDto>
                .Unauthorized(
                    "User is not authenticated.");
        }


        // ========================================================
        // Staff
        // ========================================================

        var isStaff =
            _currentUser.IsInRole("Admin") ||
            _currentUser.IsInRole("DepartmentHead") ||
            _currentUser.IsInRole("Employee");


        if (!isStaff)
        {
            return ServiceResult<TicketStatisticsDto>
                .Forbidden(
                    "Only staff can view ticket statistics.");
        }


        // ========================================================
        // Base Query
        // ========================================================

        IQueryable<Ticket> query =
            _uow.Repository<Ticket>()
                .Query()
                .AsNoTracking();


        // مهم جدًا:
        // نفس Security Boundary المستخدم في باقي العمليات

        query =
            ApplyAccessFilter(query);


        var now =
            DateTime.UtcNow;


        // ========================================================
        // 1. Counts Per Status
        // ========================================================

        var byStatus =
            await query
                .GroupBy(t =>
                    new
                    {
                        t.StatusId,
                        StatusCode =
                            t.Status.Code
                    })
                .Select(g =>
                    new TicketStatusCountDto
                    {
                        StatusId =
                            g.Key.StatusId,

                        StatusCode =
                            g.Key.StatusCode,

                        Count =
                            g.Count()
                    })
                .OrderBy(x =>
                    x.StatusCode)
                .ToListAsync(
                    cancellationToken);


        // ========================================================
        // 2. Overdue
        // ========================================================

        var overdueCount =
            await query.CountAsync(
                t =>
                    t.DueAt.HasValue &&

                    t.DueAt.Value < now &&

                    t.Status.Code !=
                    "Resolved" &&

                    t.Status.Code !=
                    "Closed" &&

                    t.Status.Code !=
                    "Cancelled",
                cancellationToken);


        // ========================================================
        // 3. Unassigned
        // ========================================================

        var unassignedCount =
            await query.CountAsync(
                t =>
                    t.AssignedToUserId ==
                    null,
                cancellationToken);


        // ========================================================
        // 4. Average Resolution Time
        //
        // لا نستخدم:
        //
        // (ResolvedAt - CreatedAt).TotalHours
        //
        // لأنه لا يترجم إلى SQL.
        //
        // نستخدم DateDiffMinute ثم نحول إلى ساعات.
        // ========================================================

        var averageResolutionMinutes =
            await query
                .Where(t =>
                    t.ResolvedAt.HasValue)
                .AverageAsync(
                    t =>
                        (double?)
                        EF.Functions.DateDiffMinute(
                            t.CreatedAt,
                            t.ResolvedAt!.Value),
                    cancellationToken);


        double? averageResolutionHours =
            averageResolutionMinutes.HasValue
                ? averageResolutionMinutes.Value / 60.0
                : null;


        // ========================================================
        // 5. Statistics Per Department
        //
        // مهم:
        // لا نضع Department.Name داخل GroupBy.
        //
        // GroupBy فقط على DepartmentId.
        // ========================================================

        var departmentAggregates =
            await query
                .GroupBy(t =>
                    t.DepartmentId)
                .Select(g =>
                    new
                    {
                        DepartmentId =
                            g.Key,

                        TicketCount =
                            g.Count(),

                        OverdueCount =
                            g.Count(t =>
                                t.DueAt.HasValue &&

                                t.DueAt.Value < now &&

                                t.Status.Code !=
                                "Resolved" &&

                                t.Status.Code !=
                                "Closed" &&

                                t.Status.Code !=
                                "Cancelled"),

                        UnassignedCount =
                            g.Count(t =>
                                t.AssignedToUserId ==
                                null),

                        AverageResolutionMinutes =
                            g.Where(t =>
                                    t.ResolvedAt.HasValue)
                             .Average(t =>
                                 (double?)
                                 EF.Functions.DateDiffMinute(
                                     t.CreatedAt,
                                     t.ResolvedAt!.Value))
                    })
                .ToListAsync(
                    cancellationToken);


        // ========================================================
        // 6. Get Department Names Separately
        // ========================================================

        var departmentIds =
            departmentAggregates
                .Select(x =>
                    x.DepartmentId)
                .Distinct()
                .ToList();


        var departments =
            await _uow.Repository<Department>()
                .Query()
                .AsNoTracking()
                .Where(d =>
                    departmentIds.Contains(
                        d.Id))
                .Select(d =>
                    new
                    {
                        d.Id,
                        d.Name
                    })
                .ToListAsync(
                    cancellationToken);


        var departmentNames =
            departments.ToDictionary(
                d => d.Id,
                d => d.Name);


        // ========================================================
        // 7. Build Department DTOs In Memory
        // ========================================================

        var byDepartment =
            departmentAggregates
                .Select(x =>
                {
                    departmentNames.TryGetValue(
                        x.DepartmentId,
                        out var departmentName);

                    double? averageHours =
                        x.AverageResolutionMinutes.HasValue
                            ? x.AverageResolutionMinutes.Value /
                              60.0
                            : null;

                    return new TicketDepartmentStatisticsDto
                    {
                        DepartmentId =
                            x.DepartmentId,

                        DepartmentName =
                            departmentName ??
                            "Unknown",

                        TicketCount =
                            x.TicketCount,

                        OverdueCount =
                            x.OverdueCount,

                        UnassignedCount =
                            x.UnassignedCount,

                        AverageResolutionHours =
                            averageHours
                    };
                })
                .OrderBy(x =>
                    x.DepartmentName)
                .ToList();


        // ========================================================
        // 8. Final Result
        // ========================================================

        var result =
            new TicketStatisticsDto
            {
                ByStatus =
                    byStatus,

                OverdueCount =
                    overdueCount,

                UnassignedCount =
                    unassignedCount,

                AverageResolutionHours =
                    averageResolutionHours,

                ByDepartment =
                    byDepartment
            };


        return ServiceResult<TicketStatisticsDto>
            .Success(
                result);
    }


    // ============================================================
    // BuildDetailDto
    // ============================================================

    private static TicketDetailDto BuildDetailDto(
        Ticket ticket)
    {
        return new TicketDetailDto
        {
            Id =
                ticket.Id,

            TicketNumber =
                ticket.TicketNumber,

            Title =
                ticket.Title,

            Description =
                ticket.Description,

            StatusCode =
                ticket.Status.Code,

            PriorityCode =
                ticket.Priority.Code,

            CategoryName =
                ticket.Category.Name,

            DepartmentName =
                ticket.Department.Name,

            SubmittedByName =
                ticket.SubmittedByUser.UserName ??
                ticket.SubmittedByUser.Email ??
                "Unknown",

            AssignedToName =
                ticket.AssignedToUser?.UserName ??
                ticket.AssignedToUser?.Email,

            CreatedAt =
                ticket.CreatedAt,

            DueAt =
                ticket.DueAt,

            IsOverdue =
                ticket.DueAt.HasValue &&

                ticket.DueAt.Value <
                DateTime.UtcNow &&

                ticket.Status.Code !=
                "Resolved" &&

                ticket.Status.Code !=
                "Closed" &&

                ticket.Status.Code !=
                "Cancelled",

            RowVersion =
                Convert.ToBase64String(
                    ticket.RowVersion)
        };
    }


    // ============================================================
    // Workflow Engine — Helpers
    //
    // كل الميثودز التالية تتعامل مع جداول:
    // WorkflowDefinition / WorkflowStep /
    // WorkflowInstance / WorkflowStepInstance
    //
    // وهي منفصلة تماماً عن ITicketWorkflow (قواعد انتقال حالة
    // التذكرة Open/InProgress/...)، والتي تبقى كما هي.
    // ============================================================

    // ------------------------------------------------------------
    // GetApplicableWorkflowDefinitionAsync
    //
    // يبحث أولاً عن قالب مخصص لهذا القسم + هذه الفئة تحديداً.
    // لو ما في، يرجع للقالب الافتراضي العام لهذا القسم
    // (CategoryId = null, IsDefault = true).
    // ------------------------------------------------------------

    private async Task<WorkflowDefinition?>
        GetApplicableWorkflowDefinitionAsync(
            int departmentId,
            int categoryId,
            CancellationToken cancellationToken)
    {
        var specificDefinition =
            await _uow.Repository<WorkflowDefinition>()
                .Query()
                .AsNoTracking()
                .Where(d =>
                    d.DepartmentId == departmentId &&
                    d.CategoryId == categoryId)
                .FirstOrDefaultAsync(cancellationToken);

        if (specificDefinition is not null)
        {
            return specificDefinition;
        }

        return await _uow.Repository<WorkflowDefinition>()
            .Query()
            .AsNoTracking()
            .Where(d =>
                d.DepartmentId == departmentId &&
                d.CategoryId == null &&
                d.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);
    }


    // ------------------------------------------------------------
    // CreateWorkflowInstanceAsync
    //
    // يُستدعى من CreateTicketAsync مباشرة بعد إنشاء التذكرة.
    // ينشئ WorkflowInstance + أول WorkflowStepInstance
    // (أصغر StepOrder بالقالب المطابق).
    //
    // لو ما في قالب مهيأ لهذا القسم/الفئة، يتجاهل الأمر بأمان
    // (لا يوقف إنشاء التذكرة — الـ Workflow ميزة اختيارية).
    // ------------------------------------------------------------

    private async Task CreateWorkflowInstanceAsync(
        Ticket ticket,
        CancellationToken cancellationToken)
    {
        var definition =
            await GetApplicableWorkflowDefinitionAsync(
                ticket.DepartmentId,
                ticket.CategoryId,
                cancellationToken);

        if (definition is null)
        {
            return;
        }

        var firstStep =
            await _uow.Repository<WorkflowStep>()
                .Query()
                .AsNoTracking()
                .Where(s =>
                    s.WorkflowDefinitionId ==
                    definition.Id)
                .OrderBy(s => s.StepOrder)
                .FirstOrDefaultAsync(cancellationToken);

        if (firstStep is null)
        {
            return;
        }

        var workflowInstance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            WorkflowDefinitionId = definition.Id,
            Status = WorkflowInstanceStatusInProgress
        };

        await _uow.Repository<WorkflowInstance>()
            .AddAsync(
                workflowInstance,
                cancellationToken);

        var firstStepInstance = new WorkflowStepInstance
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstance.Id,
            WorkflowStepId = firstStep.Id,
            StepOrder = firstStep.StepOrder,
            Status = WorkflowStepStatusInProgress,
            AssignedToUserId = firstStep.AssignedUserId
        };

        await _uow.Repository<WorkflowStepInstance>()
            .AddAsync(
                firstStepInstance,
                cancellationToken);
    }


    // ------------------------------------------------------------
    // AdvanceWorkflowInstanceAsync
    //
    // يُستدعى من UpdateTicketStatusAsync بعد تأكيد صحة الانتقال.
    //
    //  - لو الحالة الجديدة نهائية (Resolved/Closed/Cancelled):
    //    نقفل الخطوة الحالية والـ WorkflowInstance كامل.
    //
    //  - لو الحالة الجديدة "InProgress" وفي خطوة حالية شغالة:
    //    نعتبرها اكتملت وننشئ الخطوة التالية بالقالب (إن وجدت).
    // ------------------------------------------------------------

    private async Task AdvanceWorkflowInstanceAsync(
        Guid ticketId,
        string newStatusCode,
        CancellationToken cancellationToken)
    {
        var workflowInstance =
            await _uow.Repository<WorkflowInstance>()
                .Query()
                .Include(wi => wi.StepInstances)
                .FirstOrDefaultAsync(
                    wi => wi.TicketId == ticketId,
                    cancellationToken);

        if (workflowInstance is null)
        {
            // لا يوجد Workflow مرتبط بهذه التذكرة
            // (لم يُهيأ قالب لقسمها/فئتها) — تجاهل بأمان.
            return;
        }

        var currentStepInstance =
            workflowInstance.StepInstances
                .Where(si =>
                    si.Status ==
                    WorkflowStepStatusInProgress)
                .OrderBy(si => si.StepOrder)
                .FirstOrDefault();


        // --------------------------------------------------------
        // حالة نهائية للتذكرة تقفل الـ Workflow كامل
        // --------------------------------------------------------

        if (newStatusCode is "Resolved" or "Closed" or "Cancelled")
        {
            if (currentStepInstance is not null)
            {
                currentStepInstance.Status =
                    newStatusCode == "Cancelled"
                        ? WorkflowStepStatusCancelled
                        : WorkflowStepStatusCompleted;
            }

            workflowInstance.Status =
                newStatusCode == "Cancelled"
                    ? WorkflowInstanceStatusCancelled
                    : WorkflowInstanceStatusCompleted;

            return;
        }


        // --------------------------------------------------------
        // انتقال للأمام (مثلاً Open → InProgress) يُنهي الخطوة
        // الحالية وينشئ التالية بالترتيب (إن وجدت)
        // --------------------------------------------------------

        if (newStatusCode == "InProgress" &&
            currentStepInstance is not null)
        {
            currentStepInstance.Status =
                WorkflowStepStatusCompleted;

            var nextStep =
                await _uow.Repository<WorkflowStep>()
                    .Query()
                    .AsNoTracking()
                    .Where(s =>
                        s.WorkflowDefinitionId ==
                        workflowInstance.WorkflowDefinitionId &&

                        s.StepOrder >
                        currentStepInstance.StepOrder)
                    .OrderBy(s => s.StepOrder)
                    .FirstOrDefaultAsync(cancellationToken);

            if (nextStep is not null)
            {
                var nextStepInstance = new WorkflowStepInstance
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = workflowInstance.Id,
                    WorkflowStepId = nextStep.Id,
                    StepOrder = nextStep.StepOrder,
                    Status = WorkflowStepStatusInProgress,
                    AssignedToUserId = nextStep.AssignedUserId
                };

                await _uow.Repository<WorkflowStepInstance>()
                    .AddAsync(
                        nextStepInstance,
                        cancellationToken);
            }
        }
    }


    // ------------------------------------------------------------
    // ReopenWorkflowInstanceAsync
    //
    // يُستدعى من ReopenTicketAsync — يعيد فتح آخر خطوة كانت
    // مكتملة على الـ WorkflowInstance المرتبط (إن وجد).
    // ------------------------------------------------------------

    private async Task ReopenWorkflowInstanceAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var workflowInstance =
            await _uow.Repository<WorkflowInstance>()
                .Query()
                .Include(wi => wi.StepInstances)
                .FirstOrDefaultAsync(
                    wi => wi.TicketId == ticketId,
                    cancellationToken);

        if (workflowInstance is null)
        {
            return;
        }

        workflowInstance.Status =
            WorkflowInstanceStatusInProgress;

        var lastCompletedStep =
            workflowInstance.StepInstances
                .Where(si =>
                    si.Status ==
                    WorkflowStepStatusCompleted)
                .OrderByDescending(si => si.StepOrder)
                .FirstOrDefault();

        if (lastCompletedStep is not null)
        {
            lastCompletedStep.Status =
                WorkflowStepStatusInProgress;
        }
    }


    // ------------------------------------------------------------
    // SyncWorkflowStepAssigneeAsync
    //
    // يُستدعى من AssignTicketAsync / AutoAssignTicketAsync عند
    // تعيين/إزالة موظف تنفيذ — يزامن AssignedToUserId على خطوة
    // سير العمل الحالية (WorkflowStepInstance بحالة InProgress).
    // ------------------------------------------------------------

    private async Task SyncWorkflowStepAssigneeAsync(
        Guid ticketId,
        Guid? assignedToUserId,
        CancellationToken cancellationToken)
    {
        var workflowInstance =
            await _uow.Repository<WorkflowInstance>()
                .Query()
                .Include(wi => wi.StepInstances)
                .FirstOrDefaultAsync(
                    wi => wi.TicketId == ticketId,
                    cancellationToken);

        var currentStepInstance =
            workflowInstance?.StepInstances
                .Where(si =>
                    si.Status ==
                    WorkflowStepStatusInProgress)
                .OrderBy(si => si.StepOrder)
                .FirstOrDefault();

        if (currentStepInstance is not null)
        {
            currentStepInstance.AssignedToUserId =
                assignedToUserId;
        }
    }
}