using BusinessLogic.ServiceResult;
using Contract.Dtos;
using Result = BusinessLogic.ServiceResult.ServiceResult;

namespace BusinessLogic.Abstractions;

public interface ICategoryService
{
    Task<ServiceResult<PagedResult<CategoryDto>>> GetAllAsync(
        PagedQuery query,
        int? departmentId = null,
        bool? active = null,
        CancellationToken ct = default);

    Task<ServiceResult<IReadOnlyList<CategoryLookupDto>>> GetLookupAsync(
        int? departmentId = null,
        CancellationToken ct = default);

    Task<ServiceResult<CategoryDto>> GetByIdAsync(
        int id,
        CancellationToken ct = default);

    Task<ServiceResult<CategoryDto>> CreateAsync(
        CreateCategoryDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<CategoryDto>> UpdateAsync(
        int id,
        UpdateCategoryDto dto,
        CancellationToken ct = default);

    Task<Result> DeleteAsync(
        int id,
        CancellationToken ct = default);
}