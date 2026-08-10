using BusinessLogic.Abstractions;
using BusinessLogic.ServiceResult;
using Contract.Dtos;
using DataAccess.Context;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Result = BusinessLogic.ServiceResult.ServiceResult;

namespace BusinessLogic.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;

    public UserService(
        AppDbContext context,
        UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ServiceResult<PagedResult<UserDto>>> GetAllAsync(
        PagedQuery query,
        string? search = null,
        CancellationToken ct = default)
    {
        var users = _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();

            users = users.Where(u =>
                (u.UserName != null &&
                 u.UserName.Contains(text)) ||

                (u.Email != null &&
                 u.Email.Contains(text)) ||

                (u.PhoneNumber != null &&
                 u.PhoneNumber.Contains(text)));
        }

        users = users.OrderBy(u => u.UserName);

        var totalCount =
            await users.CountAsync(ct);

        var items = await users
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,

                UserName =
                    u.UserName ?? string.Empty,

                Email =
                    u.Email ?? string.Empty,

                PhoneNumber =
                    u.PhoneNumber,

                DepartmentId =
                    u.PrimaryDepartmentId,

                DepartmentName =
                    u.PrimaryDepartment == null
                        ? string.Empty
                        : u.PrimaryDepartment.Name,

                IsActive =
                    u.IsActive,

                Roles =
                    u.UserRoles
                        .Where(ur => !ur.IsDeleted)
                        .Select(ur => ur.Role.Code)
                        .ToList()
            })
            .ToListAsync(ct);

        var result =
            new PagedResult<UserDto>(
                items,
                query.Page,
                query.PageSize,
                totalCount);

        return ServiceResult<PagedResult<UserDto>>
            .Success(result);
    }

    public async Task<ServiceResult<IReadOnlyList<UserLookupDto>>> GetLookupAsync(
        CancellationToken ct = default)
    {
        IReadOnlyList<UserLookupDto> users =
            await _context.Users
                .AsNoTracking()
                .Where(u =>
                    !u.IsDeleted &&
                    u.IsActive)
                .OrderBy(u => u.UserName)
                .Select(u => new UserLookupDto
                {
                    Id = u.Id,

                    Name =
                        u.UserName
                        ?? u.Email
                        ?? string.Empty
                })
                .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<UserLookupDto>>
            .Success(users);
    }

    public async Task<ServiceResult<UserDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u =>
                u.Id == id &&
                !u.IsDeleted)
            .Select(u => new UserDto
            {
                Id = u.Id,

                UserName =
                    u.UserName ?? string.Empty,

                Email =
                    u.Email ?? string.Empty,

                PhoneNumber =
                    u.PhoneNumber,

                DepartmentId =
                    u.PrimaryDepartmentId,

                DepartmentName =
                    u.PrimaryDepartment == null
                        ? string.Empty
                        : u.PrimaryDepartment.Name,

                IsActive =
                    u.IsActive,

                Roles =
                    u.UserRoles
                        .Where(ur => !ur.IsDeleted)
                        .Select(ur => ur.Role.Code)
                        .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            return ServiceResult<UserDto>
                .NotFound("User not found.");
        }

        return ServiceResult<UserDto>
            .Success(user);
    }

    public async Task<ServiceResult<UserDto>> CreateAsync(
        CreateUserDto dto,
        CancellationToken ct = default)
    {
        var email = dto.Email.Trim();

        var existingUser =
            await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return ServiceResult<UserDto>
                .Conflict(
                    "A user with this email already exists.");
        }

        if (dto.DepartmentId.HasValue)
        {
            var departmentExists =
                await _context.Departments
                    .AnyAsync(
                        d =>
                            d.Id == dto.DepartmentId.Value &&
                            !d.IsDeleted,
                        ct);

            if (!departmentExists)
            {
                return ServiceResult<UserDto>
                    .BadRequest(
                        "Department does not exist.");
            }
        }

        var roleCodes = dto.Roles
            .Where(r =>
                !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        var roles = await _context
            .Set<Role>()
            .Where(r =>
                !r.IsDeleted &&
                roleCodes.Contains(r.Code))
            .ToListAsync(ct);

        if (roles.Count != roleCodes.Count)
        {
            return ServiceResult<UserDto>
                .BadRequest(
                    "One or more roles do not exist.");
        }

        var needsDepartment =
            roles.Any(r =>
                r.IsDepartmentScoped);

        if (needsDepartment &&
            !dto.DepartmentId.HasValue)
        {
            return ServiceResult<UserDto>
                .BadRequest(
                    "A department is required for department-scoped roles.");
        }

        var isAgent =
            roles.Any(r =>
                r.Code == "Agent");

        var user = new User
        {
            UserName = email,
            Email = email,
            PhoneNumber = dto.PhoneNumber,
            PrimaryDepartmentId =
                dto.DepartmentId,

            IsActive = true,

            UserType =
                isAgent
                    ? "Employee"
                    : "Citizen"
        };

        var createResult =
            await _userManager.CreateAsync(
                user,
                dto.Password);

        if (!createResult.Succeeded)
        {
            var message = string.Join(
                " ",
                createResult.Errors
                    .Select(e => e.Description));

            return ServiceResult<UserDto>
                .BadRequest(message);
        }

        foreach (var role in roles)
        {
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,

                DepartmentId =
                    role.IsDepartmentScoped
                        ? dto.DepartmentId
                        : null
            };

            await _context.UserRoles
                .AddAsync(
                    userRole,
                    ct);
        }

        if (isAgent)
        {
            if (!dto.DepartmentId.HasValue)
            {
                return ServiceResult<UserDto>
                    .BadRequest(
                        "Agent must have a department.");
            }

            var agent = new Agent
            {
                UserId = user.Id,

                DepartmentId =
                    dto.DepartmentId.Value
            };

            await _context.Agents
                .AddAsync(
                    agent,
                    ct);
        }

        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(
            user.Id,
            ct);
    }

    public async Task<ServiceResult<UserDto>> UpdateAsync(
        Guid id,
        UpdateUserDto dto,
        CancellationToken ct = default)
    {
        var user =
            await _userManager.FindByIdAsync(
                id.ToString());

        if (user is null ||
            user.IsDeleted)
        {
            return ServiceResult<UserDto>
                .NotFound("User not found.");
        }

        var email = dto.Email.Trim();

        var userWithEmail =
            await _userManager
                .FindByEmailAsync(email);

        if (userWithEmail is not null &&
            userWithEmail.Id != id)
        {
            return ServiceResult<UserDto>
                .Conflict(
                    "A user with this email already exists.");
        }

        if (dto.DepartmentId.HasValue)
        {
            var departmentExists =
                await _context.Departments
                    .AnyAsync(
                        d =>
                            d.Id == dto.DepartmentId.Value &&
                            !d.IsDeleted,
                        ct);

            if (!departmentExists)
            {
                return ServiceResult<UserDto>
                    .BadRequest(
                        "Department does not exist.");
            }
        }

        var roleCodes = dto.Roles
            .Where(r =>
                !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();

        var roles = await _context
            .Set<Role>()
            .Where(r =>
                !r.IsDeleted &&
                roleCodes.Contains(r.Code))
            .ToListAsync(ct);

        if (roles.Count != roleCodes.Count)
        {
            return ServiceResult<UserDto>
                .BadRequest(
                    "One or more roles do not exist.");
        }

        var needsDepartment =
            roles.Any(r =>
                r.IsDepartmentScoped);

        if (needsDepartment &&
            !dto.DepartmentId.HasValue)
        {
            return ServiceResult<UserDto>
                .BadRequest(
                    "A department is required for department-scoped roles.");
        }

        var wantsAgent =
            roles.Any(r =>
                r.Code == "Agent");

        var existingAgent =
            await _context.Agents
                .FirstOrDefaultAsync(
                    a =>
                        a.UserId == id,
                    ct);

        if (!wantsAgent &&
            existingAgent is not null &&
            !existingAgent.IsDeleted)
        {
            var hasOpenTickets =
                await _context.Tickets
                    .AnyAsync(
                        t =>
                            t.AssignedAgentId ==
                                existingAgent.Id &&
                            !t.IsDeleted &&
                            !t.Status.IsTerminal,
                        ct);

            if (hasOpenTickets)
            {
                return ServiceResult<UserDto>
                    .Conflict(
                        "The user is still assigned to open tickets. Reassign them before removing the Agent role.");
            }
        }

        user.Email = email;
        user.UserName = email;
        user.PhoneNumber =
            dto.PhoneNumber;

        user.PrimaryDepartmentId =
            dto.DepartmentId;

        user.UserType =
            wantsAgent
                ? "Employee"
                : "Citizen";

        var updateResult =
            await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var message = string.Join(
                " ",
                updateResult.Errors
                    .Select(e => e.Description));

            return ServiceResult<UserDto>
                .BadRequest(message);
        }

        var existingUserRoles =
            await _context.UserRoles
                .Where(ur =>
                    ur.UserId == id)
                .ToListAsync(ct);

        foreach (var userRole in existingUserRoles)
        {
            userRole.IsDeleted = true;
        }

        foreach (var role in roles)
        {
            Guid? departmentId =
                role.IsDepartmentScoped
                    ? dto.DepartmentId
                    : null;

            var existingUserRole =
                existingUserRoles
                    .FirstOrDefault(ur =>
                        ur.RoleId == role.Id &&
                        ur.DepartmentId == departmentId);

            if (existingUserRole is not null)
            {
                existingUserRole.IsDeleted = false;
            }
            else
            {
                var newUserRole =
                    new UserRole
                    {
                        UserId = id,
                        RoleId = role.Id,
                        DepartmentId = departmentId
                    };

                await _context.UserRoles
                    .AddAsync(
                        newUserRole,
                        ct);
            }
        }

        if (wantsAgent)
        {
            if (!dto.DepartmentId.HasValue)
            {
                return ServiceResult<UserDto>
                    .BadRequest(
                        "Agent must have a department.");
            }

            if (existingAgent is null)
            {
                existingAgent =
                    new Agent
                    {
                        UserId = id,
                        DepartmentId =
                            dto.DepartmentId.Value
                    };

                await _context.Agents
                    .AddAsync(
                        existingAgent,
                        ct);
            }
            else
            {
                existingAgent.DepartmentId =
                    dto.DepartmentId.Value;

                existingAgent.IsDeleted =
                    false;
            }
        }
        else if (existingAgent is not null)
        {
            existingAgent.IsDeleted = true;
        }

        await _context.SaveChangesAsync(ct);

        await _userManager
            .UpdateSecurityStampAsync(user);

        return await GetByIdAsync(
            id,
            ct);
    }

    public async Task<Result> DeactivateAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var user =
            await _userManager
                .FindByIdAsync(
                    id.ToString());

        if (user is null ||
            user.IsDeleted)
        {
            return Result.NotFound(
                "User not found.");
        }

        user.IsActive = false;

        var updateResult =
            await _userManager
                .UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var message = string.Join(
                " ",
                updateResult.Errors
                    .Select(e => e.Description));

            return Result.BadRequest(message);
        }

        await _userManager
            .UpdateSecurityStampAsync(user);

        return Result.NoContent();
    }

    public async Task<Result> ActivateAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var user =
            await _userManager
                .FindByIdAsync(
                    id.ToString());

        if (user is null ||
            user.IsDeleted)
        {
            return Result.NotFound(
                "User not found.");
        }

        user.IsActive = true;

        var updateResult =
            await _userManager
                .UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var message = string.Join(
                " ",
                updateResult.Errors
                    .Select(e => e.Description));

            return Result.BadRequest(message);
        }

        await _userManager
            .UpdateSecurityStampAsync(user);

        return Result.NoContent();
    }

    public async Task<Result> ResetPasswordAsync(
        Guid id,
        ResetUserPasswordDto dto,
        CancellationToken ct = default)
    {
        var user =
            await _userManager
                .FindByIdAsync(
                    id.ToString());

        if (user is null ||
            user.IsDeleted)
        {
            return Result.NotFound(
                "User not found.");
        }

        var token =
            await _userManager
                .GeneratePasswordResetTokenAsync(
                    user);

        var resetResult =
            await _userManager
                .ResetPasswordAsync(
                    user,
                    token,
                    dto.NewPassword);

        if (!resetResult.Succeeded)
        {
            var message = string.Join(
                " ",
                resetResult.Errors
                    .Select(e => e.Description));

            return Result.BadRequest(message);
        }

        await _userManager
            .UpdateSecurityStampAsync(user);

        return Result.NoContent();
    }
}