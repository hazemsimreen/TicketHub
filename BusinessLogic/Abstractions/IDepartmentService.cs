using BusinessLogic.Common;
using Contract.Dtos;
using Result = BusinessLogic.Common.ServiceResult;

namespace BusinessLogic.Abstractions;

public interface IDepartmentService
{
    Task<ServiceResult<PagedResult<DepartmentDto>>> GetAllAsync(
        PagedQuery query,
        CancellationToken ct = default);

    Task<ServiceResult<DepartmentDto>> GetByIdAsync(
        int id,
        CancellationToken ct = default);

    Task<ServiceResult<DepartmentDto>> CreateAsync(
        CreateDepartmentDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<DepartmentDto>> UpdateAsync(
        int id,
        UpdateDepartmentDto dto,
        CancellationToken ct = default);

    Task<Result> DeleteAsync(
        int id,
        CancellationToken ct = default);
}
