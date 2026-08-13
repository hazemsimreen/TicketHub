using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using TicketHub.DataAccess.Repositories;
using Result = BusinessLogic.Common.ServiceResult;
using Contract.Paged;
namespace BusinessLogic.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<PagedResult<DepartmentDto>>> GetAllAsync(
        PagedQuery query,
        CancellationToken ct = default)
    {
        var departments = _unitOfWork
            .Repository<Department>()
            .Query()
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Name);

        var totalCount = await departments.CountAsync(ct);

        var items = await departments
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                ParentDepartmentId = d.ParentDepartmentId,

                CategoryCount = d.Categories
                    .Count(c => !c.IsDeleted),

                AgentCount = d.Agents
                    .Count(a => !a.IsDeleted),

                OpenTicketCount = d.Tickets
                    .Count(t =>
                        !t.IsDeleted &&
                        !t.Status.IsTerminal)
            })
            .ToListAsync(ct);

        var result = new PagedResult<DepartmentDto>(
            items,
            query.Page,
            query.PageSize,
            totalCount);

        return ServiceResult<PagedResult<DepartmentDto>>
            .Success(result);
    }

    public async Task<ServiceResult<DepartmentDto>> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        var department = await _unitOfWork
            .Repository<Department>()
            .Query()
            .AsNoTracking()
            .Where(d =>
                d.Id == id &&
                !d.IsDeleted)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                ParentDepartmentId = d.ParentDepartmentId,

                CategoryCount = d.Categories
                    .Count(c => !c.IsDeleted),

                AgentCount = d.Agents
                    .Count(a => !a.IsDeleted),

                OpenTicketCount = d.Tickets
                    .Count(t =>
                        !t.IsDeleted &&
                        !t.Status.IsTerminal)
            })
            .FirstOrDefaultAsync(ct);

        if (department is null)
        {
            return ServiceResult<DepartmentDto>
                .NotFound("Department not found.");
        }

        return ServiceResult<DepartmentDto>
            .Success(department);
    }

    public async Task<ServiceResult<DepartmentDto>> CreateAsync(
        CreateDepartmentDto dto,
        CancellationToken ct = default)
    {
        var repo = _unitOfWork
            .Repository<Department>();

        var name = dto.Name.Trim();
        var code = dto.Code.Trim();

        var nameExists = await repo.ExistsAsync(
            d =>
                d.Name == name &&
                !d.IsDeleted,
            ct);

        if (nameExists)
        {
            return ServiceResult<DepartmentDto>
                .Conflict(
                    "Department name already exists.");
        }

        var codeExists = await repo.ExistsAsync(
            d =>
                d.Code == code &&
                !d.IsDeleted,
            ct);

        if (codeExists)
        {
            return ServiceResult<DepartmentDto>
                .Conflict(
                    "Department code already exists.");
        }

        if (dto.ParentDepartmentId.HasValue)
        {
            var parentExists = await repo.ExistsAsync(
                d =>
                    d.Id == dto.ParentDepartmentId.Value &&
                    !d.IsDeleted,
                ct);

            if (!parentExists)
            {
                return ServiceResult<DepartmentDto>
                    .BadRequest(
                        "Parent department does not exist.");
            }
        }

        var department = new Department
        {
            Code = code,
            Name = name,
            ParentDepartmentId =
                dto.ParentDepartmentId
        };

        await repo.AddAsync(
            department,
            ct);

        await _unitOfWork
            .SaveChangesAsync(ct);

        var result = new DepartmentDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            ParentDepartmentId =
                department.ParentDepartmentId,
            CategoryCount = 0,
            AgentCount = 0,
            OpenTicketCount = 0
        };

        return ServiceResult<DepartmentDto>
            .Created(result);
    }

    public async Task<ServiceResult<DepartmentDto>> UpdateAsync(
        int id,
        UpdateDepartmentDto dto,
        CancellationToken ct = default)
    {
        var repo = _unitOfWork
            .Repository<Department>();

        var department = await repo
            .Query()
            .FirstOrDefaultAsync(
                d =>
                    d.Id == id &&
                    !d.IsDeleted,
                ct);

        if (department is null)
        {
            return ServiceResult<DepartmentDto>
                .NotFound(
                    "Department not found.");
        }

        var name = dto.Name.Trim();
        var code = dto.Code.Trim();

        var nameExists = await repo.ExistsAsync(
            d =>
                d.Id != id &&
                d.Name == name &&
                !d.IsDeleted,
            ct);

        if (nameExists)
        {
            return ServiceResult<DepartmentDto>
                .Conflict(
                    "Department name already exists.");
        }

        var codeExists = await repo.ExistsAsync(
            d =>
                d.Id != id &&
                d.Code == code &&
                !d.IsDeleted,
            ct);

        if (codeExists)
        {
            return ServiceResult<DepartmentDto>
                .Conflict(
                    "Department code already exists.");
        }

        if (dto.ParentDepartmentId == id)
        {
            return ServiceResult<DepartmentDto>
                .BadRequest(
                    "A department cannot be its own parent.");
        }

        if (dto.ParentDepartmentId.HasValue)
        {
            var parentExists = await repo.ExistsAsync(
                d =>
                    d.Id ==
                        dto.ParentDepartmentId.Value &&
                    !d.IsDeleted,
                ct);

            if (!parentExists)
            {
                return ServiceResult<DepartmentDto>
                    .BadRequest(
                        "Parent department does not exist.");
            }
        }

        department.Code = code;
        department.Name = name;
        department.ParentDepartmentId =
            dto.ParentDepartmentId;

        repo.Update(department);

        await _unitOfWork
            .SaveChangesAsync(ct);

        return await GetByIdAsync(
            id,
            ct);
    }

    public async Task<Result> DeleteAsync(
        int id,
        CancellationToken ct = default)
    {
        var repo = _unitOfWork
            .Repository<Department>();

        var department = await repo
            .Query()
            .FirstOrDefaultAsync(
                d =>
                    d.Id == id &&
                    !d.IsDeleted,
                ct);

        if (department is null)
        {
            return Result.NotFound(
                "Department not found.");
        }

        var hasCategories = await _unitOfWork
            .Repository<Category>()
            .Query()
            .AnyAsync(
                c =>
                    c.DepartmentId == id &&
                    !c.IsDeleted,
                ct);

        if (hasCategories)
        {
            return Result.Conflict(
                "Department still has categories. Move or deactivate them first.");
        }

        repo.Remove(department);

        await _unitOfWork
            .SaveChangesAsync(ct);

        return Result.NoContent();
    }
}
