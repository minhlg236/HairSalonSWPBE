using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.AspNetCore.Authorization;

namespace hair_harmony_be.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RoleController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
        {
            return await _context.Roles.ToListAsync();
        }

        [HttpGet("all")]
        [Authorize(Policy = "admin")]
        public async Task<ActionResult<IEnumerable<Role>>> GetAllRoles()
        {
            return await _context.Roles.ToListAsync();
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "admin")]
        public async Task<ActionResult<Role>> GetRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound("Role not found.");
            }

            return role;
        }

        [HttpPost]
        [Authorize(Policy = "admin")]
        public async Task<ActionResult<Role>> CreateRole([FromBody] Role role)
        {
            if (role == null)
            {
                return BadRequest("Role data is invalid.");
            }

            role.CreatedOn = DateTime.UtcNow;
            role.UpdatedOn = DateTime.UtcNow;

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
        }

        [HttpPut("update/{id}")]
        [Authorize(Policy = "admin")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] Role role)
        {
            if (id != role.Id)
            {
                return BadRequest("Role ID mismatch.");
            }

            var existingRole = await _context.Roles.FindAsync(id);

            if (existingRole == null)
            {
                return NotFound("Role not found.");
            }

            existingRole.Title = role.Title ?? existingRole.Title;
            existingRole.Status = role.Status;
            existingRole.UpdatedOn = DateTime.UtcNow;

            _context.Entry(existingRole).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("delete/{id}")]
        [Authorize(Policy = "admin")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound("Role not found.");
            }

            role.Status = false;
            role.UpdatedOn = DateTime.UtcNow;

            _context.Entry(role).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
