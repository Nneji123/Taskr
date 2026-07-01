using Microsoft.AspNetCore.Authorization;

namespace API.Common;

public class ResourceOwnerRequirement : IAuthorizationRequirement;

public class ResourceOwnerHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<ResourceOwnerRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ResourceOwnerRequirement requirement)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        if (userId != null) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
