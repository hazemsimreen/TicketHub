using BusinessLogic;
using BusinessLogic.Abstractions;
using BusinessLogic.Common;
using Contract.Dtos;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using TicketHub.DataAccess.Repositories;
using Contract.Paged;
using Result = BusinessLogic.Common.ServiceResult;

namespace BusinessLogic.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CategoryService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    private bool IsDepartmentScopedStaff =>
        !_currentUser.IsInRole("Admin") &&
        (_currentUser.IsInRole("Agent") ||
         _currentUser.IsInRole("Supervisor"));

    public async Task<ServiceResult<PagedResult<CategoryDto>>> GetAllAsync(
        PagedQuery query,
        int? departmentId = null,
        bool? active = null,
        CancellationToken ct = default)
    {
        if (IsDepartmentScopedStaff)
        {
            if (!_currentUser.PrimaryDepartmentId.HasValue)
            {
                return ServiceResult<PagedResult<CategoryDto>>
                    .Forbidden("Department is required for this user.");
            }

            if (departmentId.HasValue &&
                departmentId.Value != _currentUser.PrimaryDepartmentId.Value)
            {
                return ServiceResult<PagedResult<CategoryDto>>
                    .Forbidden("You cannot access categories outside your department.");
            }

            departmentId = _currentUser.PrimaryDepartmentId.Value;
        }

        var categories = _unitOfWork
            .Repository<Category>()
            .Query()
            .AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (departmentId.HasValue)
        {
            categories = categories.Where(
                c => c.DepartmentId == departmentId.Value);
        }

        if (active.HasValue)
        {
            categories = categories.Where(
                c => c.IsActive == active.Value);
        }

        categories = categories.OrderBy(c => c.Name);

        var totalCount =
            await categories.CountAsync(ct);

        var items = await categories
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                DepartmentId = c.DepartmentId,
                DepartmentName = c.Department.Name,
                DefaultPriorityId = c.DefaultPriorityId,
                IsActive = c.IsActive
            })
            .ToListAsync(ct);

        var result =
            new PagedResult<CategoryDto>(
                items,
                query.Page,
                query.PageSize,
                totalCount);

        return ServiceResult<PagedResult<CategoryDto>>
            .Success(result);
    }

    public async Task<ServiceResult<IReadOnlyList<CategoryLookupDto>>> GetLookupAsync(
        int? departmentId = null,
        CancellationToken ct = default)
    {
        if (IsDepartmentScopedStaff)
        {
            if (!_currentUser.PrimaryDepartmentId.HasValue)
            {
                return ServiceResult<IReadOnlyList<CategoryLookupDto>>
                    .Forbidden("Department is required for this user.");
            }

            if (departmentId.HasValue &&
                departmentId.Value != _currentUser.PrimaryDepartmentId.Value)
            {
                return ServiceResult<IReadOnlyList<CategoryLookupDto>>
                    .Forbidden("You cannot access categories outside your department.");
            }

            departmentId = _currentUser.PrimaryDepartmentId.Value;
        }

        var categories = _unitOfWork
            .Repository<Category>()
            .Query()
            .AsNoTracking()
            .Where(c =>
                !c.IsDeleted &&
                c.IsActive);

        if (departmentId.HasValue)
        {
            categories = categories.Where(
                c => c.DepartmentId == departmentId.Value);
        }

        IReadOnlyList<CategoryLookupDto> items =
            await categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryLookupDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<CategoryLookupDto>>
            .Success(items);
    }

    public async Task<ServiceResult<CategoryDto>> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        var category = await _unitOfWork
            .Repository<Category>()
            .Query()
            .AsNoTracking()
            .Where(c =>
                c.Id == id &&
                !c.IsDeleted)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                DepartmentId = c.DepartmentId,
                DepartmentName = c.Department.Name,
                DefaultPriorityId = c.DefaultPriorityId,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(ct);

        if (category is null)
        {
            return ServiceResult<CategoryDto>
                .NotFound("Category not found.");
        }

        if (IsDepartmentScopedStaff)
        {
            if (!_currentUser.PrimaryDepartmentId.HasValue ||
                category.DepartmentId != _currentUser.PrimaryDepartmentId.Value)
            {
                return ServiceResult<CategoryDto>
                    .Forbidden("You cannot access categories outside your department.");
            }
        }

        return ServiceResult<CategoryDto>
            .Success(category);
    }

    public async Task<ServiceResult<CategoryDto>> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken ct = default)
    {
        if (IsDepartmentScopedStaff)
        {
            if (!_currentUser.PrimaryDepartmentId.HasValue ||
                dto.DepartmentId != _currentUser.PrimaryDepartmentId.Value)
            {
                return ServiceResult<CategoryDto>
                    .Forbidden("You cannot create categories outside your department.");
            }
        }

        var categoryRepo =
            _unitOfWork.Repository<Category>();

        var name = dto.Name.Trim();

        var departmentExists =
            await _unitOfWork
                .Repository<Department>()
                .ExistsAsync(
                    d =>
                        d.Id == dto.DepartmentId &&
                        !d.IsDeleted,
                    ct);

        if (!departmentExists)
        {
            return ServiceResult<CategoryDto>
                .BadRequest(
                    "Department does not exist.");
        }

        var nameExists =
            await categoryRepo.ExistsAsync(
                c =>
                    c.DepartmentId == dto.DepartmentId &&
                    c.Name == name &&
                    !c.IsDeleted,
                ct);

        if (nameExists)
        {
            return ServiceResult<CategoryDto>
                .Conflict(
                    "Category name already exists in this department.");
        }

        if (dto.DefaultPriorityId.HasValue)
        {
            var priorityExists =
                await _unitOfWork
                    .Repository<TicketPriority>()
                    .ExistsAsync(
                        p =>
                            p.Id ==
                            dto.DefaultPriorityId.Value &&
                            !p.IsDeleted,
                        ct);

            if (!priorityExists)
            {
                return ServiceResult<CategoryDto>
                    .BadRequest(
                        "Default priority does not exist.");
            }
        }

        var category = new Category
        {
            Name = name,
            DepartmentId = dto.DepartmentId,
            DefaultPriorityId =
                dto.DefaultPriorityId,
            IsActive = true
        };

        await categoryRepo.AddAsync(
            category,
            ct);

        await _unitOfWork
            .SaveChangesAsync(ct);

        var result =
            await GetByIdAsync(
                category.Id,
                ct);

        if (!result.IsSuccess ||
            result.Data is null)
        {
            return ServiceResult<CategoryDto>
                .BadRequest(
                    "Could not load created category.");
        }

        return ServiceResult<CategoryDto>
            .Created(result.Data);
    }

    public async Task<ServiceResult<CategoryDto>> UpdateAsync(
        int id,
        UpdateCategoryDto dto,
        CancellationToken ct = default)
    {
        var categoryRepo =
            _unitOfWork.Repository<Category>();

        var category =
            await categoryRepo
                .Query()
                .FirstOrDefaultAsync(
                    c =>
                        c.Id == id &&
                        !c.IsDeleted,
                    ct);

        if (category is null)
        {
            return ServiceResult<CategoryDto>
                .NotFound(
                    "Category not found.");
        }

        if (IsDepartmentScopedStaff)
        {
            if (!_currentUser.PrimaryDepartmentId.HasValue ||
                category.DepartmentId != _currentUser.PrimaryDepartmentId.Value ||
                dto.DepartmentId != _currentUser.PrimaryDepartmentId.Value)
            {
                return ServiceResult<CategoryDto>
                    .Forbidden("You cannot update categories outside your department.");
            }
        }

        var departmentExists =
            await _unitOfWork
                .Repository<Department>()
                .ExistsAsync(
                    d =>
                        d.Id == dto.DepartmentId &&
                        !d.IsDeleted,
                    ct);

        if (!departmentExists)
        {
            return ServiceResult<CategoryDto>
                .BadRequest(
                    "Department does not exist.");
        }

        var name = dto.Name.Trim();

        var nameExists =
            await categoryRepo.ExistsAsync(
                c =>
                    c.Id != id &&
                    c.DepartmentId == dto.DepartmentId &&
                    c.Name == name &&
                    !c.IsDeleted,
                ct);

        if (nameExists)
        {
            return ServiceResult<CategoryDto>
                .Conflict(
                    "Category name already exists in this department.");
        }

        if (dto.DefaultPriorityId.HasValue)
        {
            var priorityExists =
                await _unitOfWork
                    .Repository<TicketPriority>()
                    .ExistsAsync(
                        p =>
                            p.Id ==
                            dto.DefaultPriorityId.Value &&
                            !p.IsDeleted,
                        ct);

            if (!priorityExists)
            {
                return ServiceResult<CategoryDto>
                    .BadRequest(
                        "Default priority does not exist.");
            }
        }

        category.Name = name;
        category.DepartmentId =
            dto.DepartmentId;
        category.DefaultPriorityId =
            dto.DefaultPriorityId;
        category.IsActive =
            dto.IsActive;

        categoryRepo.Update(category);

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
        var categoryRepo =
            _unitOfWork.Repository<Category>();

        var category =
            await categoryRepo
                .Query()
                .FirstOrDefaultAsync(
                    c =>
                        c.Id == id &&
                        !c.IsDeleted,
                    ct);

        if (category is null)
        {
            return Result.NotFound(
                "Category not found.");
        }

        var hasTickets =
            await _unitOfWork
                .Repository<Ticket>()
                .Query()
                .IgnoreQueryFilters()
                .AnyAsync(
                    t => t.CategoryId == id,
                    ct);

        if (hasTickets)
        {
            return Result.Conflict(
                "Category is used by tickets. Deactivate it instead.");
        }

        categoryRepo.Remove(category);

        await _unitOfWork
            .SaveChangesAsync(ct);

        return Result.NoContent();
    }
}
