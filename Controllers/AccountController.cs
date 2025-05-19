using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NexusAstralis.Data;
using NexusAstralis.Interface;
using NexusAstralis.Models.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace NexusAstralis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(IEmailSender emailSender, SignInManager<NexusUser> signInManager, UserManager<NexusUser> userManager, UserContext context, NexusStarsContext starsContext, IConfiguration configuration) : ControllerBase
    {
        private readonly string GoogleClientId = Environment.GetEnvironmentVariable("Google-Client-Id")!; // Reemplaza con tu Client ID.
        private readonly string MicrosoftClientId = Environment.GetEnvironmentVariable("Microsoft-Client-Id")!; // Reemplaza con tu Client ID.

        [HttpPost("GoogleLogin")]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLogin request)
        {
            var token = request.Token;

            try
            {
                // Verifico el token de Google
                var payload = await ValidateAsync(token, new ValidationSettings
                {
                    Audience = [GoogleClientId] // Validar contra el ClientID de Google.
                });

                NexusUser user = await VerifyUser(payload.Email, payload.Name, payload.Picture);

                var localToken = await GenerateToken(user);

                return Ok(new
                {
                    Message = "Inicio de Sesión Exitoso",
                    Token = new JwtSecurityTokenHandler().WriteToken(localToken),
                    user.Email,
                    user.Name,
                    user.ProfileImage
                });
            }
            catch (InvalidJwtException ex)
            {
                // Token inválido
                return BadRequest(new { Message = "Token Inválido", Error = ex.Message });
            }
        }

        [HttpPost("MicrosoftLogin")]
        public async Task<IActionResult> MicrosoftLogin([FromBody] ExternalLogin request)
        {
            var token = request.Token;
            try
            {
                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever());

                var config = await configManager.GetConfigurationAsync();
                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    IssuerValidator = (issuer, securityToken, validationParameters) =>
                    {
                        // Validar que el emisor sea de Microsoft
                        if (issuer.StartsWith("https://login.microsoftonline.com/") && issuer.EndsWith("/v2.0"))
                        {
                            return issuer; // Emisor válido
                        }

                        // Validar emisores de la versión 1.0
                        if (issuer.StartsWith("https://sts.windows.net/"))
                        {
                            return issuer; // Emisor válido para v1.0
                        }

                        throw new SecurityTokenInvalidIssuerException("Emisor no válido.");
                    },
                    ValidateAudience = true,
                    ValidAudiences = [MicrosoftClientId],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = config.SigningKeys,
                    ValidateLifetime = true
                };

                // Validar el token
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                NexusUser user = await VerifyUser(claimsPrincipal.FindFirst("preferred_username")?.Value!,
                    claimsPrincipal.FindFirst("name")?.Value!,
                    claimsPrincipal.FindFirst("homeAccountId")?.Value!);

                var localToken = await GenerateToken(user);

                return Ok(new
                {
                    message = "Login Exitoso",
                    Token = new JwtSecurityTokenHandler().WriteToken(localToken),
                    user.Email,
                    user.Name,
                    user.ProfileImage
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Token Inválido", Error = ex.Message });
            }
        }

        private async Task<NexusUser> VerifyUser(string email, string name, string picture)
        {
            NexusUser? user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new NexusUser
                {
                    UserName = email,
                    Email = email,
                    Name = name,
                    Surname1 = "",
                    PhoneNumber = "",
                    Bday = DateOnly.FromDateTime(DateTime.Now),
                    EmailConfirmed = true,
                    ProfileImage = picture,
                    PublicProfile = false
                };
                IdentityResult result = await userManager.CreateAsync(user, "Pass-1234");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Basic");
                }
            }
            return user;
        }

        [HttpGet("GetUserId/{id}")]
        public async Task<IActionResult> GetUserId(string id)
        {
            var user = await userManager.Users
                .Where(u => u.Id == id)
                .Select(u => new UserInfoDto
                {
                    Nick = u.Nick,
                    Name = u.Name,
                    Surname1 = u.Surname1,
                    Surname2 = u.Surname2,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    ProfileImage = u.ProfileImage,
                    Bday = u.Bday,
                    About = u.About,
                    UserLocation = u.UserLocation,
                    PublicProfile = u.PublicProfile,
                    Favorites = u.Favorites
                })
                .FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }
            return Ok(user);
        }

        [HttpGet("Users")]
        public async Task<IActionResult> Users() =>
            Ok(await userManager.Users.ToListAsync());

        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers()
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }
            return Ok(userManager.Users);
        }

        [HttpGet("GetUsersLilInfo")]
        public async Task<IActionResult> GetUsersLil()
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            var users = await userManager.Users
                .Where(u => u.Id != user.Id)
                .Select(u => new
                {
                    u.Nick,
                    u.ProfileImage,
                    u.PublicProfile
                })
                .ToListAsync();

                return Ok(users);
            }

        [HttpGet("GetUserInfo/{nick}")]
        public async Task<IActionResult> GetUser(string nick)
        {
            var user = await userManager.Users
            .Include(u => u.Favorites)
            .FirstOrDefaultAsync(u => u.Nick == nick);

            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            // Obtener los IDs de las constelaciones favoritas
            var favoriteConstellationIds = user.Favorites.Select(f => f.ConstellationId).ToList();

            // Consultar la información de las constelaciones favoritas
            var favoriteConstellations = await starsContext.constellations
                .Where(c => favoriteConstellationIds.Contains(c.id))
                .ToListAsync();

            // Puedes proyectar a un DTO si lo prefieres
            var userInfo = new UserInfoDto
            {
                Nick = user.Nick,
                Name = user.Name,
                Surname1 = user.Surname1,
                Surname2 = user.Surname2,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfileImage = user.ProfileImage,
                Bday = user.Bday,
                About = user.About,
                UserLocation = user.UserLocation,
                PublicProfile = user.PublicProfile,
                // Incluye la info de las constelaciones favoritas
                Constellations = favoriteConstellations
            };

            return Ok(userInfo);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(Login model)
        {
            NexusUser? user = await userManager.FindByEmailAsync(model.Email!);

            if (user == null)
            {
                return NotFound("ERROR: No Existe un Usuario Registrado con ese E-mail.");
            }

            if (!user.EmailConfirmed)
            {
                return BadRequest("ERROR: El E-mail no Está Confirmado, Por Favor Confirma tu Registro.");
            }

            if (user != null && await userManager.CheckPasswordAsync(user, model.Password!))
            {
                var token = await GenerateToken(user);
                return Ok(new JwtSecurityTokenHandler().WriteToken(token));
            }

            return Unauthorized();
        }

        private async Task<JwtSecurityToken> GenerateToken(NexusUser user)
        {
            IList<string> roles = await userManager.GetRolesAsync(user);
            if (roles.Count == 0)
            {
                await userManager.AddToRoleAsync(user, "Basic");
                roles = await userManager.GetRolesAsync(user);
            }
            var claims = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Sub, user.UserName!),
                new (JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            return new JwtSecurityToken(
                issuer: configuration["JWT:Issuer"],
                audience: configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds);
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPassword model)
        {
            var user = await userManager.FindByEmailAsync(model.Email!);
            if (user != null)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPassword", "Account", new { email = model.Email, token }, Request.Scheme);
                await emailSender.SendEmailAsync(model.Email!, "Reestablecer Contraseña", $"Por Favor Reestablece tu Contraseña Haciendo Click en Este Enlace: <a href='{resetLink}'>Reestablecer Contraseña</a>");
                return Ok("Por Favor Revisa tu E-mail Para Cambiar la Contraseña.");
            }

            return NotFound("ERROR: No Existe un Usuario Registrado con ese E-mail.");
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPassword model)
        {
            var user = await userManager.FindByEmailAsync(model.Email!);
            if (user == null)
            {
                return NotFound("ERROR: No Existe un Usuario Registrado con ese E-mail.");
            }

            var result = await userManager.ResetPasswordAsync(user, model.Token!, model.Password!);
            return result.Succeeded
                ? Ok("Contraseña Cambiada Correctamente.")
                : BadRequest("ERROR: La Contraseña Tiene que Tener al Menos una Letra Mayúscula, una Minúscula, un Dígito, un Caracer Especial y 8 Caracteres de Longitud.");
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromForm] Register model)
        {
            if (await userManager.FindByEmailAsync(model.Email!) != null || await userManager.Users.AnyAsync(u => u.Nick == model.Nick))
            {
                return BadRequest("ERROR: Ya Existe un Usuario Registrado con ese E-mail o Nick.");
            }

            var profileImagePath = await SaveProfileImageAsync(model.ProfileImageFile, model.Nick!);

            bool Profile = model.PublicProfile == "1";

            string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

            model.Surname2 = NullIfEmpty(model.Surname2!);
            model.About = NullIfEmpty(model.About!);
            model.UserLocation = NullIfEmpty(model.UserLocation!);

            var user = new NexusUser
            {
                Nick = model.Nick,
                Name = model.Name,
                Surname1 = model.Surname1,
                Surname2 = model.Surname2,
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                ProfileImage = profileImagePath,
                Bday = model.Bday,
                About = model.About,
                UserLocation = model.UserLocation,
                PublicProfile = Profile
            };

            try
            {
                IdentityResult result = await userManager.CreateAsync(user, model.Password!);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Basic");
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);

                    await emailSender.SendEmailAsync(user.Email!, "Confirma tu Registro", $"Por Favor Confirma tu Cuenta Haciendo Click en Este Enlace: <a href='{confirmationLink}'>Confirmar Registro</a>");

                    return Ok("Confirma tu Registro.");
                }
            }
            catch (Exception)
            {
                return BadRequest("ERROR: La Contraseña Tiene que Tener al Menos una Letra Mayúscula, una Minúscula, un Dígito, un Caracer Especial y 8 Caracteres de Longitud.");
            }

            return BadRequest("ERROR: La Contraseña Tiene que Tener al Menos una Letra Mayúscula, una Minúscula, un Dígito, un Caracer Especial y 8 Caracteres de Longitud.");
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest("ERROR: La Id y el Token no Pueden Estar Vacios.");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("ERROR: No Existe un Usuario con esa ID.");
            }

            var result = await userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded
                ? Redirect("https://nexus-astralis-2.vercel.app")
                : BadRequest("ERROR: El E-mail de Confirmación no Está Registrado, ¿Estás Seguro que no Eliminaste tu Cuenta?.");
        }

        [HttpPatch("Update")]
        public async Task<IActionResult> Update([FromForm] Register model)
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

            model.Surname2 = NullIfEmpty(model.Surname2!);
            model.About = NullIfEmpty(model.About!);
            model.UserLocation = NullIfEmpty(model.UserLocation!);

            bool Profile = model.PublicProfile == "1";

            user.Nick = model.Nick;
            user.Name = model.Name;
            user.Surname1 = model.Surname1;
            user.Surname2 = model.Surname2;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Bday = model.Bday;
            user.About = model.About;
            user.UserLocation = model.UserLocation;
            user.PublicProfile = Profile;

            if (model.ProfileImageFile != null)
            {
                user.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile, model.Nick!);
            }

            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.Password))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordChangeResult = await userManager.ResetPasswordAsync(user, token, model.Password);
                    if (!passwordChangeResult.Succeeded)
                    {
                        foreach (var error in passwordChangeResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }

                        return BadRequest("ERROR: La Contraseña Tiene que Tener al Menos una Letra Mayúscula, una Minúscula, un Dígito, un Caracer Especial y 8 Caracteres de Longitud.");
                    }
                }

                await Logout();

                return Ok("Datos Actualizados.");
            }

            return BadRequest("ERROR: La Contraseña Tiene que Tener al Menos una Letra Mayúscula, una Minúscula, un Dígito, un Caracer Especial y 8 Caracteres de Longitud.");
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();

            return Ok("Loged Out.");
        }

        [HttpDelete("Delete")]
        public async Task<IActionResult> Delete()
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest("ERROR: No se Pudo Eliminar el Usuario.");
            }

            var userDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imgs/profile", user.Nick!);
            if (Directory.Exists(userDirectory))
            {
                Directory.Delete(userDirectory, true);
            }

            await Logout();
            return Ok("Usuario Eliminado.");
        }

        [HttpGet("Profile")]
        public async Task<IActionResult> Profile()
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }
            var userProfile = new UserInfoDto
            {
                Nick = user.Nick,
                Name = user.Name,
                Surname1 = user.Surname1,
                Surname2 = user.Surname2,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfileImage = user.ProfileImage,
                Bday = user.Bday,
                About = user.About,
                UserLocation = user.UserLocation,
                PublicProfile = user.PublicProfile,
                Favorites = user.Favorites
            };

            return Ok(userProfile);
        }

        [HttpGet("Favorites")]
        public async Task<IActionResult> Favorites()
        {
            var user = await GetUserFromToken();

            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            var favoriteIds = await context.Favorites
                .Where(f => f.UserId == user.Id)
                .Select(f => f.ConstellationId)
                .ToListAsync();

            return Ok(favoriteIds);
        }

        [HttpPost("Favorites")]
        public async Task<IActionResult> AddToFavorites([FromBody] int constellationId)
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            // Verificar si ya existe el favorito
            bool exists = await context.Favorites
                .AnyAsync(f => f.UserId == user.Id && f.ConstellationId == constellationId);

            if (exists)
            {
                return BadRequest("La constelación ya está en tus favoritos.");
            }

            var favorite = new Favorites
            {
                UserId = user.Id,
                ConstellationId = constellationId
            };

            context.Favorites.Add(favorite);
            await context.SaveChangesAsync();

            return Ok("Constelación Agregada a Favoritos.");
        }

        [HttpDelete("Favorites")]
        public async Task<IActionResult> DeleteFavorite([FromBody] List<int> constellationIds)
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            if (constellationIds == null || !constellationIds.Any())
            {
                return BadRequest("Debes especificar al menos una constelación.");
            }

            var favoritesToRemove = await context.Favorites
                .Where(f => f.UserId == user.Id && constellationIds.Contains(f.ConstellationId))
                .ToListAsync();

            if (!favoritesToRemove.Any())
            {
                return NotFound("No se encontraron favoritos para eliminar.");
            }

            context.Favorites.RemoveRange(favoritesToRemove);
            await context.SaveChangesAsync();

            return Ok("Constelación(es) eliminada(s) de Favoritos.");
        }

        [HttpGet("GetComments/{nick}")]
        public async Task<IActionResult> GetComments(string nick)
        {
            var userExists = await userManager.Users.AnyAsync(u => u.Nick == nick);
            if (!userExists)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            // Consultamos directamente en la tabla de comentarios del contexto de estrellas
            var userComments = await starsContext.Comments
                .Where(c => c.UserNick == nick)
                .Include(c => c.Constellation)
                .Select(c => new
                {
                    c.Id,
                    c.UserNick,
                    c.Comment,
                    ConstellationId = c.ConstellationId,
                    ConstellationName = c.Constellation.spanish_name,
                    ConstellationImage = c.Constellation.image_url
                })
                .ToListAsync();

            return Ok(userComments);
        }

        // Método auxiliar para obtener el usuario desde el token
        private async Task<NexusUser?> GetUserFromToken()
        {
            var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return null;
            }

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

                return userNameClaim == null ? null : await userManager.FindByNameAsync(userNameClaim.Value);
            }
            catch
            {
                return null;
            }
        }

        [HttpPost("UpgradeToPremium")]
        public async Task<IActionResult> UpgradeToPremium()
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            var roles = await userManager.GetRolesAsync(user);

            if (roles.Count != 1 || !roles.Contains("Basic"))
            {
                return BadRequest("Solo los Usuarios con el Rol Basic Pueden Convertirse en Premium.");
            }

            var removeResult = await userManager.RemoveFromRoleAsync(user, "Basic");
            if (!removeResult.Succeeded)
            {
                return BadRequest("No se Pudo Eliminar el Rol Basic.");
            }

            var addResult = await userManager.AddToRoleAsync(user, "Premium");
            if (!addResult.Succeeded)
            {
                return BadRequest("No se Pudo Asignar el Rol Premium.");
            }

            return Ok("¡Ahora Eres Usuario Premium!");
        }



        private static async Task<string> SaveProfileImageAsync(IFormFile? profileImageFile, string nick)
        {
            if (profileImageFile is null)
            {
                return "/imgs/default-profile.jpg";
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imgs/profile/" + nick);
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(profileImageFile.FileName);
            var lastName = $"Profile.{extension}";
            var filePath = Path.Combine(uploadsFolder, lastName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
                await profileImageFile.CopyToAsync(fileStream);

            return $"/imgs/profile/{nick}/{lastName}";
        }
    }
}