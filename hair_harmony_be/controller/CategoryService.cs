using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace hair_harmony_be.hair_harmony_be.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryServiceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryServiceController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("getAll")]
        public async Task<ActionResult<IEnumerable<CategoryService>>> GetCategoryServices()
        {
            var categories = await _context.CategoryServices.ToListAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryService>> GetCategoryService(int id)
        {
            var category = await _context.CategoryServices.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        [HttpPost("add")]
        [Authorize(Policy = "admin")]
        public async Task<ActionResult<CategoryService>> PostCategoryService(CategoryServiceAddDTO categoryDto)
        {
            if (categoryDto == null)
            {
                return BadRequest("Category data is required");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);

            var category = new CategoryService
            {
                Title = categoryDto.Title,
                Description = categoryDto.Description,
                CreatedBy = await _context.Users.FindAsync(userId),
                UpdatedBy = await _context.Users.FindAsync(userId),
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                Status = true
            };

            await _context.CategoryServices.AddAsync(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategoryService), new { id = category.Id }, category);
        }

        [HttpPut("update/{id}")]
        [Authorize(Policy = "admin")]
        public async Task<IActionResult> PutCategoryService(int id, CategoryServiceAddDTO categoryDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);

            var existingCategory = await _context.CategoryServices.FirstOrDefaultAsync(c => c.Id == id);
            if (existingCategory == null)
            {
                return NotFound($"CategoryService with ID {id} not found.");
            }

            existingCategory.Title = categoryDto.Title;
            existingCategory.Description = categoryDto.Description;
            existingCategory.UpdatedBy = await _context.Users.FindAsync(userId);
            existingCategory.UpdatedOn = DateTime.UtcNow;

            _context.CategoryServices.Update(existingCategory);
            await _context.SaveChangesAsync();

            return Ok(existingCategory);
        }

        [HttpPut("softDelete/{id}")]
        [Authorize(Policy = "admin")]
        public async Task<IActionResult> DeleteCategoryService(int id)
        {
            var category = await _context.CategoryServices.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            category.Status = true;
            _context.CategoryServices.Update(category);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
