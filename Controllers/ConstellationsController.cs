using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusAstralis.Data;
using NexusAstralis.Models.Stars;
using System.Text.Json;

namespace NexusAstralis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConstellationsController(NexusStarsContext context) : ControllerBase
    {

        //[AllowAnonymous]
        // GET: api/constellations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Constellations>>> Getconstellations()
        {
            return await context.constellations.ToListAsync();
        }

        // GET: api/constellations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Constellations>> Getconstellations(int id)
        {
            var constellations = await context.constellations.FindAsync(id);

            if (constellations == null)
            {
                return NotFound();
            }

            return constellations;
        }

        [HttpGet("GetArray/{id}")]
        public async Task<ActionResult<Constellations>> GetConstStars(int id)
        {
            var constellations = await context.constellations
                .Include(c => c.star)
                .FirstOrDefaultAsync(c => c.id == id);
            if (constellations == null)
            {
                return NotFound("Esa Contelación no Existe.");
            }
            return constellations;
        }

        [HttpGet("GetStars/{id}")]
        public async Task<ActionResult<IEnumerable<Stars>>> GetStars(int id)
        {
            var constellations = await context.constellations
                .Include(c => c.star)
                .FirstOrDefaultAsync(c => c.id == id);
            if (constellations == null)
            {
                return NotFound("Esa Contelación no Existe.");
            }

            var stars = constellations.star
            .Select(s => new Stars
            {
                id = s.id,
                x = s.x,
                y = s.y,
                z = s.z,
                ra = s.ra,
                dec = s.dec,
                mag = s.mag,
                proper = s.proper,
                az = s.az,
                alt = s.alt,
                spect = s.spect
            })
            .ToList();

                return Ok(stars);
        }

        // PUT: api/constellations/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> Putconstellations(int id, Constellations constellations)
        {
            if (id != constellations.id)
            {
                return BadRequest();
            }

            context.Entry(constellations).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!constellationsExists(id))
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

        // POST: api/constellations
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Constellations>> Postconstellations(Constellations constellations)
        {
            context.constellations.Add(constellations);
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (constellationsExists(constellations.id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("Getconstellations", new { id = constellations.id }, constellations);
        }

        // DELETE: api/constellations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Deleteconstellations(int id)
        {
            var constellations = await context.constellations.FindAsync(id);
            if (constellations == null)
            {
                return NotFound();
            }

            context.constellations.Remove(constellations);
            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("ConstelationLines")]
        public async Task<IActionResult> ConstellationLines()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Assets", "constellationLines.json");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("No se encontró el archivo de líneas de constelaciones.");
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            var constellationLines = JsonSerializer.Deserialize<object>(json); // Cambia 'object' por el tipo adecuado si lo tienes

            return Ok(constellationLines);
        }

        private bool constellationsExists(int id)
        {
            return context.constellations.Any(e => e.id == id);
        }
    }
}
