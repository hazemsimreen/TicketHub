using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TicketHub.DataAccess.Repositories;

namespace BusinessLogic.Services;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<User> _userManager;

    public TicketService(IUnitOfWork uow, ICurrentUser currentUser, UserManager<User> userManager)
    {
        _uow = uow;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<ServiceResult<TicketDetailDto>> CreateTicketAsync(
       CreateTicketDto dto,
       CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null ||
            !Guid.TryParse(_currentUser.UserId, out var submittedByUserId))
        {
            return ServiceResult<TicketDetailDto>.Unauthorized("User is not authenticated.");
        }

        var category = await _uow.Repository<Category>()
            .Query()
            .Include(c => c.Department)
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId, cancellationToken);

        if (category is null)
            return ServiceResult<TicketDetailDto>.NotFound("Category not found.");

        if (category.DefaultPriorityId is null)
            return ServiceResult<TicketDetailDto>.BadRequest("Category has no default priority configured.");

        var openStatus = await _uow.Repository<TicketStatus>()
            .Query()
            .FirstOrDefaultAsync(s => s.Id == 1, cancellationToken);

        if (openStatus is null)
            return ServiceResult<TicketDetailDto>.BadRequest("Ticket status 'Open' is not configured.");

        var priority = await _uow.Repository<TicketPriority>()
            .GetByIdAsync(category.DefaultPriorityId.Value, cancellationToken);

        if (priority is null)
            return ServiceResult<TicketDetailDto>.BadRequest("Default priority for this category is not configured correctly.");

        var submittedByUser = await _userManager.FindByIdAsync(submittedByUserId.ToString());

        if (submittedByUser is null)
            return ServiceResult<TicketDetailDto>.Unauthorized("User is not authenticated.");

        var ticketNumber = GenerateTicketNumber();
        var dueAt = DateTime.UtcNow.Add(GetSlaDuration(priority.Code));

        if (string.IsNullOrWhiteSpace(dto.Title))
            return ServiceResult<TicketDetailDto>.BadRequest("Title is required.");

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketNumber = ticketNumber,
            Title = dto.Title,
            Description = dto.Description,
            SubmittedByUserId = submittedByUserId,
            CategoryId = category.Id,
            DepartmentId = category.DepartmentId,
            PriorityId = priority.Id,
            StatusId = openStatus.Id,
            DueAt = dueAt,
            CreatedBy = submittedByUserId.ToString()
        };

        var statusHistory = new TicketStatusHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            FromStatusId = null,
            ToStatusId = openStatus.Id,
            ChangedByUserId = submittedByUserId
        };

        await _uow.Repository<Ticket>().AddAsync(ticket, cancellationToken);
        await _uow.Repository<TicketStatusHistory>().AddAsync(statusHistory, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var resultDto = new TicketDetailDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Title = ticket.Title,
            Description = ticket.Description,
            StatusCode = openStatus.Code,
            PriorityCode = priority.Code,
            CategoryName = category.Name,
            DepartmentName = category.Department.Name,
            SubmittedByName = submittedByUser.UserName ?? submittedByUser.Email ?? "Unknown",
            AssignedToName = null,
            CreatedAt = ticket.CreatedAt,
            DueAt = ticket.DueAt,
            IsOverdue = false,
            RowVersion = Convert.ToBase64String(ticket.RowVersion)
        };

        return ServiceResult<TicketDetailDto>.Success(resultDto);
    }

    private static string GenerateTicketNumber()
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"TKT-{datePart}-{randomPart}";
    }

    private static TimeSpan GetSlaDuration(string priorityCode)
    {
        return priorityCode switch
        {
            "Urgent" => TimeSpan.FromHours(4),
            "High" => TimeSpan.FromHours(24),
            "Medium" => TimeSpan.FromHours(72),
            "Low" => TimeSpan.FromHours(168),
            _ => TimeSpan.FromHours(72)
        };
    }
}