using BusinessLogic.ServiceResult;
using Contract.Dtos;
using Result = BusinessLogic.ServiceResult.ServiceResult;

namespace BusinessLogic.Abstractions;

public interface ICategoryService
{
    Task<ServiceResult<PagedResult<CategoryDto>>> GetAllAsync(
        PagedQuery query,
        Guid? departmentId = null,
        bool? active = null,
        CancellationToken ct = default);

    Task<ServiceResult<IReadOnlyList<CategoryLookupDto>>> GetLookupAsync(
        Guid? departmentId = null,
        CancellationToken ct = default);

    Task<ServiceResult<CategoryDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<ServiceResult<CategoryDto>> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<CategoryDto>> UpdateAsync(
        Guid id,
        UpdateCategoryDto dto,
        CancellationToken ct = default);

    Task<Result> DeleteAsync(
        Guid id,
        CancellationToken ct = default);
}