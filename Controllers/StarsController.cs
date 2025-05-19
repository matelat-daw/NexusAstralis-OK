using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusAstralis.Data;
using NexusAstralis.Models.Stars;

namespace NexusAstralis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StarsController(NexusStarsContext context) : ControllerBase
    {

        // GET: api/stars
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stars>>> Getstars()
        {
            return await context.stars.ToListAsync();
        }

        // GET: api/stars/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Stars>> Getstars(int id)
        {
            var stars = await context.stars.FindAsync(id);

            if (stars == null)
            {
                return NotFound();
            }

            return stars;
        }

        // PUT: api/stars/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> Putstars(int id, Stars stars)
        {
            if (id != stars.id)
            {
                return BadRequest();
            }

            context.Entry(stars).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!starsExists(id))
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

        // POST: api/stars
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Stars>> Poststars(Stars stars)
        {
            context.stars.Add(stars);
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (starsExists(stars.id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("Getstars", new { id = stars.id }, stars);
        }

        // DELETE: api/stars/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Deletestars(int id)
        {
            var stars = await context.stars.FindAsync(id);
            if (stars == null)
            {
                return NotFound();
            }

            context.stars.Remove(stars);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool starsExists(int id)
        {
            return context.stars.Any(e => e.id == id);
        }
    }
}
