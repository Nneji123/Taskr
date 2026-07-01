using System.Security.Claims;

namespace API.Common;

public interface ICurrentUser
{
    Guid Id { get; }
    string Email { get; }
}

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid Id
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user is null) return Guid.Empty;
            var subClaim = user.Claims.FirstOrDefault(c => c.Type is "sub" or "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            var sub = subClaim?.Value;
            return sub is not null && Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public string Email => httpContextAccessor.HttpContext?.User.FindFirstValue("email") ?? string.Empty;
}
