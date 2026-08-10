using BusinessLogic.Abstractions;
using BusinessLogic.ServiceResult;
using Contract.Dtos;
using DataAccess.Context;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using TicketHub.DataAccess.Repositories;
using Result = BusinessLogic.ServiceResult.ServiceResult;

namespace BusinessLogic.Services;

public class AgentService : IAgentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;

    public AgentService(
        IUnitOfWork unitOfWork,
        AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<ServiceResult<IReadOnlyList<AgentDto>>> GetAllAsync(
        Guid? departmentId = null,
        bool? active = null,
        bool? hasCapacity = null,
        string? skill = null,
        CancellationToken ct = default)
    {
        var agents = _unitOfWork
            .Repository<Agent>()
            .Query()
            .AsNoTracking()
            .Where(a =>
                !a.IsDeleted &&
                !a.User.IsDeleted);

        if (departmentId.HasValue)
        {
            agents = agents.Where(
                a => a.DepartmentId == departmentId.Value);
        }

        if (active.HasValue)
        {
            agents = agents.Where(
                a => a.User.IsActive == active.Value);
        }

        if (!string.IsNullOrWhiteSpace(skill))
        {
            var skillName = skill.Trim();

            agents = agents.Where(
                a => a.Skills.Any(
                    s =>
                        !s.IsDeleted &&
                        s.Name == skillName));
        }

        if (hasCapacity.HasValue)
        {
            if (hasCapacity.Value)
            {
                agents = agents.Where(
                    a =>
                        a.AssignedTickets.Count(
                            t =>
                                !t.IsDeleted &&
                                !t.Status.IsTerminal)
                        <
                        (a.Profile == null
                            ? 10
                            : a.Profile.MaxOpenTickets));
            }
            else
            {
                agents = agents.Where(
                    a =>
                        a.AssignedTickets.Count(
                            t =>
                                !t.IsDeleted &&
                                !t.Status.IsTerminal)
                        >=
                        (a.Profile == null
                            ? 10
                            : a.Profile.MaxOpenTickets));
            }
        }

        IReadOnlyList<AgentDto> items =
            await agents
                .OrderBy(a => a.User.UserName)
                .Select(a => new AgentDto
                {
                    Id = a.Id,

                    UserId = a.UserId,

                    UserName =
                        a.User.UserName ?? string.Empty,

                    Email =
                        a.User.Email ?? string.Empty,

                    DepartmentId =
                        a.DepartmentId,

                    DepartmentName =
                        a.Department.Name,

                    IsActive =
                        a.User.IsActive,

                    CurrentOpenTicketCount =
                        a.AssignedTickets.Count(
                            t =>
                                !t.IsDeleted &&
                                !t.Status.IsTerminal),

                    Profile =
                        a.Profile == null
                            ? null
                            : new AgentProfileDto
                            {
                                Id = a.Profile.Id,
                                MaxOpenTickets =
                                    a.Profile.MaxOpenTickets
                            },

                    Skills =
                        a.Skills
                            .Where(s => !s.IsDeleted)
                            .OrderBy(s => s.Name)
                            .Select(s => s.Name)
                            .ToList()
                })
                .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<AgentDto>>
            .Success(items);
    }

    public async Task<ServiceResult<AgentDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var agent = await _unitOfWork
            .Repository<Agent>()
            .Query()
            .AsNoTracking()
            .Where(a =>
                a.Id == id &&
                !a.IsDeleted)
            .Select(a => new AgentDto
            {
                Id = a.Id,

                UserId = a.UserId,

                UserName =
                    a.User.UserName ?? string.Empty,

                Email =
                    a.User.Email ?? string.Empty,

                DepartmentId =
                    a.DepartmentId,

                DepartmentName =
                    a.Department.Name,

                IsActive =
                    a.User.IsActive,

                CurrentOpenTicketCount =
                    a.AssignedTickets.Count(
                        t =>
                            !t.IsDeleted &&
                            !t.Status.IsTerminal),

                Profile =
                    a.Profile == null
                        ? null
                        : new AgentProfileDto
                        {
                            Id = a.Profile.Id,
                            MaxOpenTickets =
                                a.Profile.MaxOpenTickets
                        },

                Skills =
                    a.Skills
                        .Where(s => !s.IsDeleted)
                        .OrderBy(s => s.Name)
                        .Select(s => s.Name)
                        .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (agent is null)
        {
            return ServiceResult<AgentDto>
                .NotFound("Agent not found.");
        }

        return ServiceResult<AgentDto>
            .Success(agent);
    }

    public async Task<ServiceResult<AgentDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var agentId = await _unitOfWork
            .Repository<Agent>()
            .Query()
            .AsNoTracking()
            .Where(a =>
                a.UserId == userId &&
                !a.IsDeleted)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);

        if (!agentId.HasValue)
        {
            return ServiceResult<AgentDto>
                .NotFound("Agent not found.");
        }

        return await GetByIdAsync(
            agentId.Value,
            ct);
    }

    public async Task<ServiceResult<AgentDto>> CreateAsync(
        CreateAgentDto dto,
        CancellationToken ct = default)
    {
        var agentRepo =
            _unitOfWork.Repository<Agent>();

        var userExists = await _context.Users
            .AnyAsync(
                u =>
                    u.Id == dto.UserId &&
                    !u.IsDeleted,
                ct);

        if (!userExists)
        {
            return ServiceResult<AgentDto>
                .BadRequest(
                    "User does not exist.");
        }

        var departmentExists = await _unitOfWork
            .Repository<Department>()
            .ExistsAsync(
                d =>
                    d.Id == dto.DepartmentId &&
                    !d.IsDeleted,
                ct);

        if (!departmentExists)
        {
            return ServiceResult<AgentDto>
                .BadRequest(
                    "Department does not exist.");
        }

        var agentExists = await agentRepo
            .ExistsAsync(
                a =>
                    a.UserId == dto.UserId &&
                    !a.IsDeleted,
                ct);

        if (agentExists)
        {
            return ServiceResult<AgentDto>
                .Conflict(
                    "This user already has an agent record.");
        }

        var skillNames = dto.SkillNames
            .Where(x =>
                !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        var skills = await _unitOfWork
            .Repository<Skill>()
            .Query()
            .Where(s =>
                !s.IsDeleted &&
                skillNames.Contains(s.Name))
            .ToListAsync(ct);

        if (skills.Count != skillNames.Count)
        {
            return ServiceResult<AgentDto>
                .BadRequest(
                    "One or more skills do not exist.");
        }

        var agent = new Agent
        {
            UserId = dto.UserId,
            DepartmentId = dto.DepartmentId
        };

        foreach (var item in skills)
        {
            agent.Skills.Add(item);
        }

        await agentRepo.AddAsync(
            agent,
            ct);

        await _unitOfWork
            .SaveChangesAsync(ct);

        return await GetByIdAsync(
            agent.Id,
            ct);
    }

    public async Task<ServiceResult<AgentDto>> UpdateAsync(
        Guid id,
        UpdateAgentDto dto,
        CancellationToken ct = default)
    {
        var agentRepo =
            _unitOfWork.Repository<Agent>();

        var agent = await agentRepo
            .Query()
            .Include(a => a.Skills)
            .FirstOrDefaultAsync(
                a =>
                    a.Id == id &&
                    !a.IsDeleted,
                ct);

        if (agent is null)
        {
            return ServiceResult<AgentDto>
                .NotFound(
                    "Agent not found.");
        }

        var departmentExists = await _unitOfWork
            .Repository<Department>()
            .ExistsAsync(
                d =>
                    d.Id == dto.DepartmentId &&
                    !d.IsDeleted,
                ct);

        if (!departmentExists)
        {
            return ServiceResult<AgentDto>
                .BadRequest(
                    "Department does not exist.");
        }

        var skillNames = dto.SkillNames
            .Where(x =>
                !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        var skills = await _unitOfWork
            .Repository<Skill>()
            .Query()
            .Where(s =>
                !s.IsDeleted &&
                skillNames.Contains(s.Name))
            .ToListAsync(ct);

        if (skills.Count != skillNames.Count)
        {
            return ServiceResult<AgentDto>
                .BadRequest(
                    "One or more skills do not exist.");
        }

        agent.DepartmentId =
            dto.DepartmentId;

        agent.Skills.Clear();

        foreach (var skillItem in skills)
        {
            agent.Skills.Add(skillItem);
        }

        agentRepo.Update(agent);

        await _unitOfWork
            .SaveChangesAsync(ct);

        return await GetByIdAsync(
            id,
            ct);
    }

    public async Task<ServiceResult<AgentDto>> UpdateProfileAsync(
        Guid id,
        UpdateAgentProfileDto dto,
        CancellationToken ct = default)
    {
        var agent = await _unitOfWork
            .Repository<Agent>()
            .Query()
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(
                a =>
                    a.Id == id &&
                    !a.IsDeleted,
                ct);

        if (agent is null)
        {
            return ServiceResult<AgentDto>
                .NotFound(
                    "Agent not found.");
        }

        if (agent.Profile is null)
        {
            var profile = new AgentProfile
            {
                AgentId = agent.Id,
                MaxOpenTickets =
                    dto.MaxOpenTickets
            };

            await _unitOfWork
                .Repository<AgentProfile>()
                .AddAsync(
                    profile,
                    ct);
        }
        else
        {
            agent.Profile.MaxOpenTickets =
                dto.MaxOpenTickets;

            _unitOfWork
                .Repository<AgentProfile>()
                .Update(agent.Profile);
        }

        await _unitOfWork
            .SaveChangesAsync(ct);

        return await GetByIdAsync(
            id,
            ct);
    }

    public async Task<ServiceResult<IReadOnlyList<SkillDto>>> GetSkillsAsync(
        CancellationToken ct = default)
    {
        IReadOnlyList<SkillDto> skills =
            await _unitOfWork
                .Repository<Skill>()
                .Query()
                .AsNoTracking()
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.Name)
                .Select(s => new SkillDto
                {
                    Id = s.Id,

                    Name = s.Name,

                    AgentCount =
                        s.Agents.Count(
                            a =>
                                !a.IsDeleted &&
                                !a.User.IsDeleted &&
                                a.User.IsActive)
                })
                .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<SkillDto>>
            .Success(skills);
    }

    public async Task<Result> DeleteAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var agentRepo =
            _unitOfWork.Repository<Agent>();

        var agent = await agentRepo
            .Query()
            .FirstOrDefaultAsync(
                a =>
                    a.Id == id &&
                    !a.IsDeleted,
                ct);

        if (agent is null)
        {
            return Result.NotFound(
                "Agent not found.");
        }

        var hasOpenTickets = await _unitOfWork
            .Repository<Ticket>()
            .Query()
            .AnyAsync(
                t =>
                    t.AssignedAgentId == id &&
                    !t.IsDeleted &&
                    !t.Status.IsTerminal,
                ct);

        if (hasOpenTickets)
        {
            return Result.Conflict(
                "Agent still has open tickets. Reassign them before deleting the agent.");
        }

        agentRepo.Remove(agent);

        await _unitOfWork
            .SaveChangesAsync(ct);

        return Result.NoContent();
    }
}