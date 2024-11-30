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
            var skip = (page - 1) * pageSize;

            var imagesGroupedByStyle = await _context.Images
                .Where(i => i.Status && i.ServiceEntity != null)  
                .Include(i => i.ServiceEntity)  
                .ToListAsync();  

    
            var groupedImages = imagesGroupedByStyle
                .GroupBy(i => i.ServiceEntity.Id)  
                .ToList();

            var styles = await _context.Services
                .Where(s => s.Status)  
                .Skip(skip)
            .Take(pageSize)
                .ToListAsync();

            var totalCount = await _context.Services.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var stylesWithImages = styles.Select(s => new StyleWithImages
            {
                ServiceEnity = s,  
                Images = groupedImages
                    .Where(g => g.Key == s.Id) 
                    .Select(g => g.ToList())  
                    .FirstOrDefault() ?? new List<Image>()  
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