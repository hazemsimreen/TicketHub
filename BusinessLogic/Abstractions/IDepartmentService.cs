using BusinessLogic.ServiceResult;
using Contract.Dtos;
using Result = BusinessLogic.ServiceResult.ServiceResult;

namespace BusinessLogic.Abstractions;

public interface IDepartmentService
{
    Task<ServiceResult<PagedResult<DepartmentDto>>> GetAllAsync(
        PagedQuery query,
        CancellationToken ct = default);

    Task<ServiceResult<DepartmentDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<ServiceResult<DepartmentDto>> CreateAsync(
        CreateDepartmentDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<DepartmentDto>> UpdateAsync(
        Guid id,
        UpdateDepartmentDto dto,
        CancellationToken ct = default);

    Task<Result> DeleteAsync(
        Guid id,
        CancellationToken ct = default);
}