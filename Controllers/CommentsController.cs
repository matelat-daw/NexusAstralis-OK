using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusAstralis.Data;
using NexusAstralis.Models.User;
using NexusAstralis.Services;

namespace NexusAstralis.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController(UserContext context, NexusStarsContext starsContext, UserTokenService userTokenService) : ControllerBase
    {
        // GET: api/Comments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Comments>>> GetAllComments()
            => await context.Comments.ToListAsync();

        // GET: api/Comments/5
        [HttpGet("ById/{id}")]
        public async Task<ActionResult<Comments>> GetCommentById(int id)
        {
            var comment = await context.Comments.FindAsync(id);

            return comment is null ? NotFound() : Ok(comment);
        }

        [HttpGet("User/{userId}")]
        public async Task<ActionResult<IEnumerable<Comments>>> GetCommentsByUser(string userId)
        {
            var comments = await context.Comments
                .Where(c => c.UserId == userId).ToListAsync();
            
            return comments.Count == 0 ? NotFound() : Ok(comments);
        }

        // PUT: api/Comments/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutComment(int id, [FromBody] Comments comment)
        {
            if (id != comment.Id)
                return BadRequest();

            context.Entry(comment).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CommentExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // POST: api/Comments
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Comments>> PostComment([FromBody] Comments comment)
        {
            var user = await userTokenService.GetUserFromTokenAsync();
            if (user == null)
                return NotFound("ERROR: Ese Usuario no Existe.");

            var constellation = await starsContext.constellations
                .FirstOrDefaultAsync(c => c.id == comment.ConstellationId);

            if (constellation == null)
                return NotFound("ERROR: La constelación no existe.");

            comment.UserId = user.Id;
            comment.ConstellationName = constellation.latin_name;
            context.Comments.Add(comment);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCommentById), new { id = comment.Id }, comment);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var user = await userTokenService.GetUserFromTokenAsync();
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            var comment = await context.Comments.FindAsync(id);
            if (comment == null)
                return NotFound();

            context.Comments.Remove(comment);
            await context.SaveChangesAsync();
            return NoContent();
        }

        private bool CommentExists(int id)
        {
            return context.Comments.Any(e => e.Id == id);
        }
    }
}