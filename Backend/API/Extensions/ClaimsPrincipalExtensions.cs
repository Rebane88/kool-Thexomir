using System.Security.Claims;

namespace API.Extensions;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public Guid UserId()
        {
            var stringId = user.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value.Trim();
            return new Guid(stringId);
        }
    }
}
