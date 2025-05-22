using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusAstralis.Data;
using NexusAstralis.Models.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NexusAstralis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(SignInManager<NexusUser> signInManager, UserManager<NexusUser> userManager, UserContext context, NexusStarsContext starsContext) : ControllerBase
    {
        [HttpGet("GetUserId/{id}")] // Obtiene el Perfil y Los Favoritos de un Usuario por su ID.
        public async Task<IActionResult> GetUserId(string id)
        {
            var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            // Obtener favoritos.
            var favoriteIds = await context.Favorites
                .Where(f => f.UserId == id)
                .Select(f => f.ConstellationId)
                .ToListAsync();

            var favoriteConstellations = await starsContext.constellations
                .Where(c => favoriteIds.Contains(c.id))
                .ToListAsync();

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
                Favorites = favoriteConstellations
            };

            return Ok(userInfo);
        }

        [HttpGet("Users")] // Lista Completa de los Usuarios.
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

        [HttpGet("GetUsersLilInfo")] // Poca Info de Todos los Usuario.
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

        [HttpGet("GetUserInfo/{nick}")] // Obtiene el Perfil, Los Favoritos y Comentarios de un Usuario por su Nick.
        public async Task<IActionResult> GetUserInfo(string nick)
        {
            var loguedUser = await GetUserFromToken();
            if (loguedUser == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            var userByNick = await userManager.Users
                .FirstOrDefaultAsync(u => u.Nick == nick);

            if (userByNick == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            var id = userByNick.Id;

            // Obtener favoritos de la tabla Favorites
            var favoriteIds = await context.Favorites
                .Where(f => f.UserId == id)
                .Select(f => f.ConstellationId)
                .ToListAsync();

            // Cargar las constelaciones completas
            var favoriteConstellations = await starsContext.constellations
                .Where(c => favoriteIds.Contains(c.id))
                .ToListAsync();

            // Cargar los comentarios con información de las constelaciones
            var userComments = await context.Comments
                .Where(c => c.UserId == id)
                .Select(c => new
                {
                    c.Id,
                    c.UserNick,
                    c.ConstellationName,
                    c.Comment,
                    c.UserId,
                    c.ConstellationId
                })
                .ToListAsync();

            var userInfo = new UserInfoDto
            {
                Nick = userByNick.Nick,
                Name = userByNick.Name,
                Surname1 = userByNick.Surname1,
                Surname2 = userByNick.Surname2,
                Email = userByNick.Email,
                PhoneNumber = userByNick.PhoneNumber,
                ProfileImage = userByNick.ProfileImage,
                Bday = userByNick.Bday,
                About = userByNick.About,
                UserLocation = userByNick.UserLocation,
                PublicProfile = userByNick.PublicProfile,
                Favorites = favoriteConstellations,
                Comments = userComments
            };

            return Ok(userInfo);
        }

        [HttpPost("ResetPassword")] // Cambio de Contraseña.
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

        [HttpGet("ConfirmEmail")] // Confirmación de Registro.
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

        [HttpPatch("Update")] // Actualización de Datos.
        public async Task<IActionResult> Update([FromForm] Register model)
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            if (user.Nick != model.Nick && await NickExistsAsync(model.Nick!, user.Id))
            {
                return BadRequest("ERROR: Ya Existe un Usuario con ese Nick.");
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

        [HttpPost("Logout")] // Logout de la App.
        public async Task<IActionResult> Logout()
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }
            await signInManager.SignOutAsync();

            return Ok("Loged Out.");
        }

        [HttpDelete("Delete")] // Delete de la Cuenta. Elimina el Usuario y su Carpeta de Imágenes, por el Token.
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

        [HttpGet("Profile")] // Perfile del Usuario Logueado.
        public async Task<IActionResult> Profile()
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            // Obtener favoritos
            var favoriteIds = await context.Favorites
                .Where(f => f.UserId == user.Id)
                .Select(f => f.ConstellationId)
                .ToListAsync();

            var favoriteConstellations = await starsContext.constellations
                .Where(c => favoriteIds.Contains(c.id))
                .ToListAsync();

            // Obtener comentarios
            var userComments = await context.Comments
                .Where(c => c.UserId == user.Id)
                .Select(c => new
                {
                    c.Id,
                    c.UserNick,
                    c.ConstellationName,
                    c.Comment,
                    c.UserId,
                    c.ConstellationId
                })
                .ToListAsync();

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
                Favorites = favoriteConstellations,
                Comments = userComments
            };

            return Ok(userProfile);
        }

        [HttpGet("Favorites")] // Favoritos del Usuario Logueado.
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

            var favoriteConstellations = await starsContext.constellations
                .Where(c => favoriteIds.Contains(c.id))
                .Select(c => new
                {
                    c.id,
                    Nombre = c.latin_name,
                    Mitologia = c.mythology,
                    Imagen = c.image_url
                })
                .ToListAsync();

            return Ok(favoriteConstellations);
        }

        [HttpGet("Favorites/{id}")] // Verifica si una Constelación es Favorita.
        public async Task<IActionResult> Favorite(int id)
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }
            bool favorite = await context.Favorites
                .AnyAsync(f => f.UserId == user.Id && f.ConstellationId == id);

            return Ok(favorite);
        }

        [HttpPost("Favorites/{id}")] // Agrega una Constelación a Favoritos.
        public async Task<IActionResult> AddToFavorites(int id)
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            // Verificar si ya existe el favorito
            bool exists = await context.Favorites
                .AnyAsync(f => f.UserId == user.Id && f.ConstellationId == id);

            if (exists)
            {
                return BadRequest("La constelación ya está en tus favoritos.");
            }

            var favorite = new Favorites
            {
                UserId = user.Id,
                ConstellationId = id
            };

            context.Favorites.Add(favorite);
            await context.SaveChangesAsync();

            return Ok("Constelación Agregada a Favoritos.");
        }

        [HttpDelete("Favorites/{id}")] // Elimina una Constelación de Favoritos del Usuario Logueado.
        public async Task<IActionResult> DeleteFavorite(int id)
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            var favoriteToRemove = await context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == user.Id && f.ConstellationId == id);

            if (favoriteToRemove == null)
            {
                return NotFound("No se encontró el favorito para eliminar.");
            }

            // Eliminar el favorito encontrado
            context.Favorites.Remove(favoriteToRemove);
            await context.SaveChangesAsync();

            return Ok("Constelación Eliminada de Favoritos.");
        }

        [HttpGet("GetUserComments/{id}")] // Obtiene los Comentarios de un Usuario por su ID.
        public async Task<IActionResult> GetComments(string id)
        {
            var userExists = await userManager.Users.AnyAsync(u => u.Id == id);
            if (!userExists)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            // Consultamos directamente en la tabla de comentarios del contexto de estrellas
            var userComments = await context.Comments
                .Where(c => c.UserId == id)
                .Select(c => new
                {
                    c.Id,
                    c.UserId,
                    c.Comment,
                    c.ConstellationId
                })
                .ToListAsync();

            return Ok(userComments);
        }

        [HttpGet("GetComments/{id}")]
        public async Task<ActionResult<IEnumerable<Comments>>> GetComments(int id)
        {
            // Buscar comentarios en la base de datos de usuarios por id de constelación
            var comments = await context.Comments
                .Where(c => c.ConstellationId == id)
                .ToListAsync();

            return Ok(comments);
        }

        // Método auxiliar para obtener el usuario desde el token
        private async Task<NexusUser?> GetUserFromToken() // Obtiene el Usuario Logueado.
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

        [HttpPost("UpgradeToPremium")] // Upgrade to Premium.
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

        private async Task<bool> NickExistsAsync(string nick, string? currentUserId = null) // Comprueba si Existe el Nick del Usuario.
        {
            // Si estamos actualizando un usuario existente, excluirlo de la verificación
            if (currentUserId != null)
            {
                return await userManager.Users
                    .AnyAsync(u => u.Nick == nick && u.Id != currentUserId);
            }

            // Para nuevos usuarios, verificar si cualquier usuario tiene ese Nick
            return await userManager.Users.AnyAsync(u => u.Nick == nick);
        }

        public static async Task<string> SaveProfileImageAsync(IFormFile? profileImageFile, string nick)
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
        } // Guarda la Imagen de Perfil en el Servidor.
    }
}