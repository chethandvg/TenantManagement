using Hangfire.Dashboard;

namespace TentMan.Api.Authorization;

/// <summary>
/// Authorization filter for Hangfire Dashboard.
/// In production, this should be restricted to authenticated administrators.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // In development, allow all access for easier testing
        if (httpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
        {
            return true;
        }

        // In production, require authentication and Administrator role
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Administrator");
    }
}
