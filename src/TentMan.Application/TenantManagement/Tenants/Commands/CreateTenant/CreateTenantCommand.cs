using TentMan.Contracts.Tenants;
using TentMan.Contracts.Enums;
using MediatR;

namespace TentMan.Application.TenantManagement.Tenants.Commands.CreateTenant;

public record CreateTenantCommand(
    Guid OrgId,
    string FullName,
    string Phone,
    string? Email,
    DateOnly? DateOfBirth,
    Gender? Gender
) : IRequest<TenantListDto>;
