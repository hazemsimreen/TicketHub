using BusinessLogic.ServiceResult;
using Contract.Dtos;
using Result = BusinessLogic.ServiceResult.ServiceResult;

namespace BusinessLogic.Abstractions;

public interface IAgentService
{
    Task<ServiceResult<IReadOnlyList<AgentDto>>> GetAllAsync(
        Guid? departmentId = null,
        bool? active = null,
        bool? hasCapacity = null,
        string? skill = null,
        CancellationToken ct = default);

    Task<ServiceResult<AgentDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task<ServiceResult<AgentDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<ServiceResult<AgentDto>> CreateAsync(
        CreateAgentDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<AgentDto>> UpdateAsync(
        Guid id,
        UpdateAgentDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<AgentDto>> UpdateProfileAsync(
        Guid id,
        UpdateAgentProfileDto dto,
        CancellationToken ct = default);

    Task<ServiceResult<IReadOnlyList<SkillDto>>> GetSkillsAsync(
        CancellationToken ct = default);

    Task<Result> DeleteAsync(
        Guid id,
        CancellationToken ct = default);
}