using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace hair_harmony_be.controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ImageController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API tạo mới một Image.
        /// </summary>
        [HttpPost("create")]
        [Authorize(Policy = "admin")]  // Chỉ admin mới được phép
        public async Task<IActionResult> CreateImage([FromBody] ImageCreateRequest request)
        {
            // Kiểm tra request có hợp lệ không
            if (request == null || string.IsNullOrEmpty(request.Url))
            {
                return BadRequest("Invalid data.");
            }

            // Lấy thông tin người tạo từ JWT
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            // Kiểm tra Style có tồn tại không
            var style = await GetStyleByIdInternal(request.StyleId);
            if (style == null)
            {
                return BadRequest($"Style with ID {request.StyleId} not found.");
            }

            // Tạo mới Image
            var image = new Image
            {
                Url = request.Url,
                Style = style,
                CreatedBy = userId,
                UpdatedBy = userId,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                Status = true
            };

            // Thêm vào DbContext và lưu thay đổi
            _context.Images.Add(image);
            await _context.SaveChangesAsync();

            // Lấy thông tin Image vừa tạo
            var createdImage = await GetImage(image.Id);

            // Trả về phản hồi Created với URL hợp lệ
            return Created("", createdImage);
        }

        /// <summary>
        /// API lấy thông tin chi tiết một Image theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "admin")] // Chỉ admin mới được phép
        public async Task<IActionResult> GetImageById(int id)
        {
            var image = await _context.Images
                .Include(i => i.Style)  // Bao gồm thông tin Style
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null)
            {
                return NotFound(new { Message = "Image not found." });
            }

            return Ok(image);
        }

        // Phương thức lấy thông tin Image theo ID (dùng chung với GetImageById)
        public async Task<Image> GetImage(int id)
        {
            var image = await _context.Images
                .Include(i => i.Style)  // Bao gồm thông tin Style
                .FirstOrDefaultAsync(i => i.Id == id);

            return image;
        }

        public async Task<Style> GetStyleByIdInternal(int id)
        {
            // Lấy thông tin Style từ database, bao gồm cả Creator và Updater
            var style = await _context.Styles
                .Include(s => s.CreatedBy)
                .Include(s => s.UpdatedBy)
                .FirstOrDefaultAsync(s => s.Id == id);

            return style; // Trả về đối tượng Style hoặc null nếu không tìm thấy
        }
    }
}
