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
    public class ImageController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ImageController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("getAll")]
        public async Task<ActionResult<IEnumerable<Image>>> GetImages()
        {
            var images = await _context.Images
                .Include(i => i.ServiceEntity)
                .ToListAsync();

            return Ok(images);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Image>> GetImage(int id)
        {
            var image = await _context.Images
                .Include(i => i.ServiceEntity)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null)
            {
                return NotFound();
            }

            return Ok(image);
        }

        [HttpPost ("add")]
        [Authorize(Policy = "staff")]
        public async Task<ActionResult<Image>> PostImage(ImageAddDTO imageDto)
        {
            if (imageDto == null)
            {
                return BadRequest("Image data is required");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == imageDto.serviceId);
            if (service == null)
            {
                return NotFound($"Service with ID {imageDto.serviceId} not found.");
            }

            var image = new Image
            {
                Url = imageDto.Url,
                ServiceEntity = service,
                CreatedBy = userId,
                UpdatedBy = userId,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                Status = true
            };

            await _context.Images.AddAsync(image);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetImage), new { id = image.Id }, image);
        }

        [HttpPut("update/{id}")]
        [Authorize(Policy = "staff")]
        public async Task<IActionResult> PutImage(int id, ImageAddDTO imageDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);

            var existingImage = await _context.Images.Include(i => i.ServiceEntity).FirstOrDefaultAsync(i => i.Id == id);
            if (existingImage == null)
            {
                return NotFound($"Image with ID {id} not found.");
            }

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == imageDto.serviceId);
            if (service == null)
            {
                return NotFound($"Service with ID {imageDto.serviceId} not found.");
            }

            existingImage.Url = imageDto.Url;
            existingImage.ServiceEntity = service;
            existingImage.UpdatedBy = userId;
            existingImage.UpdatedOn = DateTime.UtcNow;

            _context.Images.Update(existingImage);
            await _context.SaveChangesAsync();

            return Ok(existingImage);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "staff")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var image = await _context.Images.FirstOrDefaultAsync(i => i.Id == id);
            if (image == null)
            {
                return NotFound();
            }

            _context.Images.Remove(image);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
