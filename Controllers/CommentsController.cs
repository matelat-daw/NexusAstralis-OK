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
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController(UserContext context, UserManager<NexusUser> userManager) : ControllerBase
    {
        // GET: api/Comments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Comments>>> GetAllComments()
        {
            return await context.Comments.ToListAsync();
        }

        // GET: api/Comments/5
        [HttpGet("ById/{id}")]
        public async Task<ActionResult<Comments>> GetCommentsById(int id)
        {
            var comments = await context.Comments.FindAsync(id);

            if (comments == null)
            {
                return NotFound();
            }

            return comments;
        }

        [HttpGet("User/{userId}")]
        public async Task<ActionResult<IEnumerable<Comments>>> GetCommentsByUser(string userId)
        {
            var comments = await context.Comments.Where(c => c.UserId == userId).ToListAsync();
            if (comments == null || comments.Count == 0)
            {
                return NotFound();
            }
            return comments;
        }

        // PUT: api/Comments/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutComments(int id, Comments comments)
        {
            if (id != comments.Id)
            {
                return BadRequest();
            }

            context.Entry(comments).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CommentsExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool CommentsExists(int id)
        {
            return context.Comments.Any(e => e.Id == id);
        }

        // POST: api/Comments
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Comments>> PostComments(Comments comments)
        {
            var user = await GetUserFromToken();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            var UserId = user.Id;
            comments.UserId = UserId;
            context.Comments.Add(comments);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetComments", new { id = comments.Id }, comments);
        }

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
    }
}