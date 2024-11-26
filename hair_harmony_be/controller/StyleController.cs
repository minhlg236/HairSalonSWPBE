using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace hair_harmony_be.controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class StyleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StyleController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API tạo mới một Style (chỉ Admin mới được phép).
        /// </summary>
        [HttpPost("create")]
        [Authorize(Policy = "admin")]
        public async Task<IActionResult> CreateStyle([FromBody] StyleCreateRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid data.");
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized("Invalid token.");
            }

            var creator = await _context.Users.FindAsync(userId);
            if (creator == null)
            {
                return BadRequest("Invalid user.");
            }

            var style = new Style
            {
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Discount = request.Discount,
                Commission = request.Commission,
                CreatedBy = creator,
                UpdatedBy = creator,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                Status = true
            };

            _context.Styles.Add(style);
            await _context.SaveChangesAsync();

            // Lấy thông tin style vừa tạo bằng phương thức nội bộ
            var createdStyle = await GetStyleByIdInternal(style.Id);

            return Created("", createdStyle);
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

        /// <summary>
        /// API lấy thông tin chi tiết của một Style theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "admin")]  // Chỉ cần xác thực người dùng
        public async Task<IActionResult> GetStyleByIdToShowMessage(int id)
        {
            try
            {
                // Tìm Style theo ID, bao gồm thông tin người tạo và người cập nhật
                var style = await _context.Styles
                     .Include(s => s.CreatedBy)
                .Include(s => s.UpdatedBy)
                    .FirstOrDefaultAsync(s => s.Id == id);

                // Nếu không tìm thấy Style, trả về lỗi 404
                if (style == null)
                {
                    return NotFound(new { Message = "Style not found." });
                }

                // Chuẩn bị dữ liệu trả về
                var result = new Style
                {
                    Id = style.Id,
                    Title = style.Title,
                    Description = style.Description,
                    Price = style.Price,
                    Discount = style.Discount,
                    Commission = style.Commission,
                    Status = style.Status,
                    CreatedOn = style.CreatedOn,
                    UpdatedOn = style.UpdatedOn,
                    CreatedBy = style.CreatedBy,
                    UpdatedBy = style.UpdatedBy,
                };

                // Trả về kết quả thành công
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Trả về lỗi 500 nếu có vấn đề trong quá trình xử lý
                return StatusCode(500, new { Message = "Internal server error.", Error = ex.Message });
            }
        }

        [HttpGet("getAll")]
        public async Task<ActionResult<PagedResult<StyleWithImages>>> GetStylesWithImages(int page = 1, int pageSize = 10)
        {
            // Tính toán số bản ghi bắt đầu và số bản ghi cần lấy (phân trang)
            var skip = (page - 1) * pageSize;

            // Lấy tất cả các Image có Style và trạng thái hợp lệ
            var imagesGroupedByStyle = await _context.Images
                .Where(i => i.Status && i.Style != null)  // Kiểm tra Style không phải null và Image có trạng thái true
                .Include(i => i.Style)  // Bao gồm Style trong kết quả
                .ToListAsync();  // Lấy tất cả dữ liệu Image về, không phân trang

            // Nhóm các Image theo StyleId
            var groupedImages = imagesGroupedByStyle
                .GroupBy(i => i.Style.Id)  // Nhóm các Image theo StyleId
                .ToList();

            // Lấy tất cả các Style trong trang hiện tại
            var styles = await _context.Styles
                .Where(s => s.Status)  // Chỉ lấy các Style có trạng thái là true
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            // Tính tổng số bản ghi của Style để tính số trang
            var totalCount = await _context.Styles.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Kết hợp các Style với Image đã nhóm
            var stylesWithImages = styles.Select(s => new StyleWithImages
            {
                StyleEnity = s,  // Style chính
                Images = groupedImages
                    .Where(g => g.Key == s.Id)  // Lọc các Image có StyleId tương ứng với Style này
                    .Select(g => g.ToList())  // Chuyển từ Group về danh sách Image
                    .FirstOrDefault() ?? new List<Image>()  // Nếu không có Image, trả về danh sách rỗng
            }).ToList();

            // Trả về kết quả phân trang
            return Ok(new PagedResult<StyleWithImages>
            {
                Items = stylesWithImages,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize
            });
        }





    }
}
