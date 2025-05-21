using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusAstralis.Data;
using NexusAstralis.Models.User;

namespace NexusAstralis.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController(UserContext context) : ControllerBase
    {

        // GET: api/Comments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Comments>>> GetComments()
        {
            return await context.Comments.ToListAsync();
        }

        // GET: api/Comments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Comments>> GetComments(int id)
        {
            var comments = await context.Comments.FindAsync(id);

            if (comments == null)
            {
                return NotFound();
            }

            return comments;
        }

        [HttpGet("User/{id}")]
        public async Task<ActionResult<IEnumerable<Comments>>> GetComments(string id)
        {
            var comments = await context.Comments.Where(c => c.UserId == id).ToListAsync();
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

        // POST: api/Comments
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Comments>> PostComments(Comments comments)
        {
            context.Comments.Add(comments);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetComments", new { id = comments.Id }, comments);
        }

        // DELETE: api/Comments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComments(int id)
        {
            var comments = await context.Comments.FindAsync(id);
            if (comments == null)
            {
                return NotFound();
            }

            context.Comments.Remove(comments);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool CommentsExists(int id)
        {
            return context.Comments.Any(e => e.Id == id);
        }
    }
}
