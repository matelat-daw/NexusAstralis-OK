using Microsoft.AspNetCore.Identity;
using NexusAstralis.Models.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NexusAstralis.Services
{
    public class UserTokenService(UserManager<NexusUser> userManager, IHttpContextAccessor httpContextAccessor)
    {
        public async Task<NexusUser?> GetUserFromTokenAsync()
        {
            var httpContext = httpContextAccessor.HttpContext;
            var authHeader = httpContext?.Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            var token = authHeader["Bearer ".Length..].Trim();

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var userNameClaim = jwtToken.Claims.FirstOrDefault(c =>
                    c.Type == JwtRegisteredClaimNames.Sub ||
                    c.Type == ClaimTypes.NameIdentifier ||
                    c.Type == "name" ||
                    c.Type == "email");

                return userNameClaim == null
                    ? null
                    : await userManager.FindByNameAsync(userNameClaim.Value);
            }
            catch
            {
                return null;
            }
        }
    }
}