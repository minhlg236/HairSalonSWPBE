using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hair_harmony_be.controller
{
    [Route("api/[controller]")]
    public class ServiceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ServiceController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("getAll")]
        public async Task<ActionResult<PagedResult<StyleWithImages>>> GetStylesWithImages(int page = 1, int pageSize = 10)
        {
            // Tính toán số bản ghi bắt đầu và số bản ghi cần lấy (phân trang)
            var skip = (page - 1) * pageSize;

            // Lấy tất cả các Image có Style và trạng thái hợp lệ
            var imagesGroupedByStyle = await _context.Images
                .Where(i => i.Status && i.ServiceEntity != null)  // Kiểm tra Style không phải null và Image có trạng thái true
                .Include(i => i.ServiceEntity)  // Bao gồm Style trong kết quả
                .ToListAsync();  // Lấy tất cả dữ liệu Image về, không phân trang

            // Nhóm các Image theo StyleId
            var groupedImages = imagesGroupedByStyle
                .GroupBy(i => i.ServiceEntity.Id)  // Nhóm các Image theo StyleId
                .ToList();

            // Lấy tất cả các Style trong trang hiện tại
            var styles = await _context.Services
                .Where(s => s.Status)  // Chỉ lấy các Style có trạng thái là true
                .Skip(skip)
            .Take(pageSize)
                .ToListAsync();

            // Tính tổng số bản ghi của Style để tính số trang
            var totalCount = await _context.Services.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Kết hợp các Style với Image đã nhóm
            var stylesWithImages = styles.Select(s => new StyleWithImages
            {
                ServiceEnity = s,  // Style chính
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
