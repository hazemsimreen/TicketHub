using BusinessLogic.Common;
using Contract.Dtos;
using Result = BusinessLogic.Common.ServiceResult;

namespace BusinessLogic.Abstractions;

public interface IUserService
{
    Task<ServiceResult<PagedResult<UserDto>>> GetAllAsync(
        PagedQuery query,
        string? search = null,
        CancellationToken ct = default);

    Task<ServiceResult<IReadOnlyList<UserLookupDto>>> GetLookupAsync(
        CancellationToken ct = default);

    Task<ServiceResult<UserDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<ServiceResult<UserDto>> CreateAsync(
        CreateUserDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<UserDto>> UpdateAsync(
        Guid id,
        UpdateUserDto dto,
        CancellationToken ct = default);

    Task<Result> DeactivateAsync(
        Guid id,
        CancellationToken ct = default);

    Task<Result> ActivateAsync(
        Guid id,
        CancellationToken ct = default);

    Task<Result> ResetPasswordAsync(
        Guid id,
        ResetUserPasswordDto dto,
        CancellationToken ct = default);
}
