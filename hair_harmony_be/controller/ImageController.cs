using Microsoft.AspNetCore.Mvc;
using hair_harmony_be.hair_harmony_be.repositoty.model;
using hair_harmony_be.hair_harmony_be.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hair_harmony_be.hair_harmony_be.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ImageRepository _imageRepository;

        public ImageController(ImageRepository imageRepository)
        {
            _imageRepository = imageRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Image>>> GetImages()
        {
            var images = await _imageRepository.GetAllImagesAsync();
            return Ok(images);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Image>> GetImage(int id)
        {
            var image = await _imageRepository.GetImageByIdAsync(id);

            if (image == null)
            {
                return NotFound();
            }

            return Ok(image);
        }

        [HttpPost]
        public async Task<ActionResult<Image>> PostImage(Image image)
        {
            if (image == null)
            {
                return BadRequest("Image data is required");
            }

            image.CreatedOn = DateTime.UtcNow;
            image.UpdatedOn = DateTime.UtcNow;

            await _imageRepository.AddImageAsync(image);

            return CreatedAtAction(nameof(GetImage), new { id = image.Id }, image);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutImage(int id, Image image)
        {
            if (id != image.Id)
            {
                return BadRequest();
            }

            var existingImage = await _imageRepository.GetImageByIdAsync(id);
            if (existingImage == null)
            {
                return NotFound();
            }

            image.UpdatedOn = DateTime.UtcNow;

            await _imageRepository.UpdateImageAsync(image);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var image = await _imageRepository.GetImageByIdAsync(id);
            if (image == null)
            {
                return NotFound();
            }

            await _imageRepository.DeleteImageAsync(id);
            return NoContent();
        }
    }
}
