using TentMan.Contracts.Tenants;
using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.TenantManagement.Tenants.Commands.UpdateTenant;

public record UpdateTenantCommand(
    Guid TenantId,
    string FullName,
    string Phone,
    string? Email,
    DateOnly? DateOfBirth,
    Gender? Gender,
    bool IsActive,
    byte[] RowVersion
) : IRequest<TenantDetailDto>;
