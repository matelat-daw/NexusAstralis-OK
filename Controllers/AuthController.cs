using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NexusAstralis.Interface;
using NexusAstralis.Models.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NexusAstralis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController (IEmailSender emailSender, UserManager<NexusUser> userManager, IConfiguration configuration) : ControllerBase
    {
        private readonly string GoogleClientId = Environment.GetEnvironmentVariable("Google-Client-Id")!; // Reemplaza con tu Client ID.
        private readonly string MicrosoftClientId = Environment.GetEnvironmentVariable("Microsoft-Client-Id")!; // Reemplaza con tu Client ID.

        [HttpPost("GoogleLogin")] // Login con Google.
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLogin request)
        {
            try
            {
                // Verifico el token de Google
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Token, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [GoogleClientId] // Validar contra el ClientID de Google.
                });

                NexusUser user = await VerifyUser(payload.Email, payload.Name, payload.Picture);

                var localToken = await GenerateToken(user);

                return Ok(new
                {
                    Message = "Inicio de Sesión Exitoso",
                    Token = new JwtSecurityTokenHandler().WriteToken(localToken),
                    user.Nick,
                    user.Email,
                    user.Name,
                    user.ProfileImage
                });
            }
            catch (InvalidJwtException ex)
            {
                return BadRequest(new { Message = "Token Inválido", Error = ex.Message });
            }
        }

        [HttpPost("MicrosoftLogin")] // Login con Microsoft.
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
                    //IssuerValidator = (issuer, securityToken, validationParameters) =>
                    IssuerValidator = (issuer, _, _) =>
                    {
                        if (issuer.StartsWith("https://login.microsoftonline.com/") && issuer.EndsWith("/v2.0"))
                            return issuer; // Emisor válido

                        if (issuer.StartsWith("https://sts.windows.net/"))
                            return issuer; // Emisor válido para v1.0

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

                NexusUser user = await VerifyUser(
                    claimsPrincipal.FindFirst("preferred_username")?.Value ?? "",
                    claimsPrincipal.FindFirst("name")?.Value ?? "",
                    claimsPrincipal.FindFirst("homeAccountId")?.Value ?? "");

                var localToken = await GenerateToken(user);

                return Ok(new
                {
                    message = "Login Exitoso",
                    Token = new JwtSecurityTokenHandler().WriteToken(localToken),
                    user.Nick,
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

        [HttpPost("Login")] // Login en la App.
        public async Task<IActionResult> Login([FromBody] Login model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                return BadRequest("ERROR: El E-mail no Puede Estar Vacío.");
            if (string.IsNullOrWhiteSpace(model.Password))
                return BadRequest("ERROR: La Contraseña no Puede Estar Vacía.");

            NexusUser? user = await userManager.FindByEmailAsync(model.Email!);

            if (user == null)
                return NotFound("Credenciales inválidas.");

            if (!user.EmailConfirmed)
                return BadRequest("ERROR: El E-mail no Está Confirmado, Por Favor Confirma tu Registro.");

            if (user != null && await userManager.CheckPasswordAsync(user, model.Password!))
            {
                var token = await GenerateToken(user);
                return Ok(new JwtSecurityTokenHandler().WriteToken(token));
            }

            return Unauthorized();
        }

        [HttpPost("Register")] // Registro en la App.
        public async Task<IActionResult> Register([FromForm] Register model)
        {
            if (await userManager.FindByEmailAsync(model.Email!) != null)
                return BadRequest("ERROR: Ya Existe un Usuario Registrado con ese E-mail.");

            if (await NickExistsAsync(model.Nick!))
                return BadRequest("ERROR: Ya Existe un Usuario Registrado con ese Nick.");

            var profileImagePath = await AccountController.SaveProfileImageAsync(model.ProfileImageFile, model.Nick!);

            bool Profile = model.PublicProfile == "1";
            string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

            var user = new NexusUser
            {
                Nick = model.Nick,
                Name = model.Name,
                Surname1 = model.Surname1,
                Surname2 = NullIfEmpty(model.Surname2!),
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                ProfileImage = profileImagePath,
                Bday = model.Bday == default ? DateOnly.FromDateTime(DateTime.Now) : model.Bday,
                About = NullIfEmpty(model.About!),
                UserLocation = NullIfEmpty(model.UserLocation!),
                PublicProfile = Profile
            };

            IdentityResult result = await userManager.CreateAsync(user, model.Password!);
            if (!result.Succeeded)
                return BadRequest("ERROR: La Contraseña Tiene que Tener al Menos una Letra Mayúscula, una Minúscula, un Dígito, un Caracer Especial y 8 Caracteres de Longitud.");

            await userManager.AddToRoleAsync(user, "Basic");
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);

            await emailSender.SendEmailAsync(user.Email!, "Confirma tu Registro", $"Por Favor Confirma tu Cuenta Haciendo Click en Este Enlace: <a href='{confirmationLink}'>Confirmar Registro</a>");

            return Ok("Confirma tu Registro.");
        }

        [HttpPost("ForgotPassword")] // Olvido de Contraseña.
        public async Task<IActionResult> ForgotPassword(ForgotPassword model)
        {
            var user = await userManager.FindByEmailAsync(model.Email!);
            if (user == null)
                return NotFound("ERROR: No Existe un Usuario Registrado con ese E-mail.");

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { email = model.Email, token }, Request.Scheme);
            await emailSender.SendEmailAsync(model.Email!, "Reestablecer Contraseña", $"Por Favor Reestablece tu Contraseña Haciendo Click en Este Enlace: <a href='{resetLink}'>Reestablecer Contraseña</a>");
            return Ok("Por Favor Revisa tu E-mail Para Cambiar la Contraseña.");
        }

        private async Task<NexusUser> VerifyUser(string email, string name, string picture) // Verifica si el Usuario ya tiene Cuenta en la App.
        {
            NexusUser? user = await userManager.FindByEmailAsync(email);
            if (user != null)
                return user;

            string baseNick = email.Split('@')[0];
            string nick = baseNick;

            // Verificar si ya existe el Nick y, en caso afirmativo, añadir un número
            int counter = 1;
            while (await NickExistsAsync(nick))
                nick = $"{baseNick}{counter++}";
                
            user = new NexusUser
            {
                Nick = nick,
                UserName = email,
                Email = email,
                Name = name,
                Surname1 = "",
                PhoneNumber = "",
                EmailConfirmed = true,
                ProfileImage = picture,
                PublicProfile = false
            };
            IdentityResult result = await userManager.CreateAsync(user, "Pass-1234");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, "Basic");
            return user;
        }

        private async Task<bool> NickExistsAsync(string nick, string? currentUserId = null) // Comprueba si Existe el Nick del Usuario.
        {
            // Si estamos actualizando un usuario existente, excluirlo de la verificación
            if (currentUserId != null)
                return await userManager.Users
                    .AnyAsync(u => u.Nick == nick && u.Id != currentUserId);

            // Para nuevos usuarios, verifica si cualquier usuario tiene ese Nick
            return await userManager.Users.AnyAsync(u => u.Nick == nick);
        }

        private async Task<JwtSecurityToken> GenerateToken(NexusUser user) // Genera el Token, se llama desde varios Métodos.
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
                expires: DateTime.UtcNow.AddMinutes(120),
                signingCredentials: creds);
        }
    }
}