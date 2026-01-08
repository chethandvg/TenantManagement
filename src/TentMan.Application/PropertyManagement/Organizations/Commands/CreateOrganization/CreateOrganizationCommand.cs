using MediatR;

namespace TentMan.Application.PropertyManagement.Organizations.Commands.CreateOrganization;

public record CreateOrganizationCommand(
    string Name,
    string TimeZone = "Asia/Kolkata"
) : IRequest<Guid>;
