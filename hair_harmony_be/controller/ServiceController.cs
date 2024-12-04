using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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

        [HttpPost("create")]
        [Authorize(Policy = "staff")]
        public async Task<IActionResult> CreateService([FromBody] ServiceCreateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);
            var creator = await _context.Users.FindAsync(userId);

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new { message = "Service name is required." });
            }
            if (request.Price <= 0)
            {
                return BadRequest(new { message = "Price must be greater than 0." });
            }
            double discount = 0.0;
            if (request.Discount.HasValue)
            {

                discount = request.Discount.Value;

                if (request.Discount.HasValue && request.Discount.Value < 0)
                {
                    return BadRequest(new { message = "Price must be greater than 0 when discount is applied." });
                }
            }

            if (request.TimeService <= 0)
            {
                return BadRequest(new { message = "TimeService must be greater than 0." });
            }

            var newService = new Service
            {
                Title = request.Title,
                Description = request.Description,
                Price = request.Price,
                Discount = request.Discount,
                TimeService = request.TimeService,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                CreatedBy = creator,
                UpdatedBy = creator
            };

            _context.Services.Add(newService);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStylesWithImages), new { id = newService.Id }, new
            {
                message = "Service created successfully.",
                service = newService
            });
        }

        [HttpPut("update/{id}")]
        [Authorize(Policy = "staff")]
        public async Task<IActionResult> UpdateService(int id, [FromBody] ServiceUpdateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);
            var updateBy = await _context.Users.FindAsync(userId);
            var existingService = await _context.Services.FirstOrDefaultAsync(s => s.Id == id);
            if (existingService == null)
            {
                return NotFound(new { message = "Service not found." });
            }

            if (request is null || (request.Title == null && request.Description == null &&
                        !request.Price.HasValue && !request.TimeService.HasValue &&
                        !request.Status.HasValue))
            {
                return BadRequest(new { message = "At least one field must be provided for update." });
            }

            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                existingService.Title = request.Title;
            }

            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                existingService.Description = request.Description;
            }

            if (request.Price.HasValue && request.Price > 0)
            {
                existingService.Price = request.Price.Value;
            }

            if (request.TimeService.HasValue && request.TimeService > 0)
            {
                existingService.TimeService = request.TimeService.Value;
            }
            if (request.Status.HasValue)
            {
                if (request.Status is bool)
                {
                    existingService.Status = request.Status.Value;
                }
                else
                {
                    return BadRequest(new { message = "Invalid status value. Status must be a boolean." });
                }
            }
            existingService.UpdatedOn = DateTime.UtcNow;
            existingService.UpdatedBy = updateBy;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Service updated successfully.", service = existingService });
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

        [HttpGet("getAllServices-not-paging")]
        public async Task<IActionResult> GetAllServicesNotPaging() // dành cho việc hiển thị filter
        {
            var services = await _context.Services.ToListAsync();

            if (!services.Any())
            {
                return Ok(new List<Service>());
            }

            return Ok(services);
        }

    }
}