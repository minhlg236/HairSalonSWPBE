using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace hair_harmony_be.controller
{
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
        }
        [HttpPost("create")]
        [Authorize(Policy = "user")]
        public async Task<IActionResult> CreateBooking([FromBody] BookingCreateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);
            var creator = await _context.Users.FindAsync(userId);

            var existingBooking = await _context.Bookings
                .Where(b => b.CreatedBy.Id == userId &&
                            b.Service.Id == request.ServiceId &&
                            (b.Status == "booked" || b.Status == "confirmed"))
                .FirstOrDefaultAsync();

            if (existingBooking != null)
            {
                return Conflict(new { message = "A booking already exists with the same service, status 'booked' or 'confirmed', and user." });
            }

            var service = await _context.Services.FindAsync(request.ServiceId);
            if (service == null)
            {
                return NotFound(new { message = "Service not found." });
            }

            if (!DateTime.TryParse(request.StartTime, out var startTime))
            {
                return BadRequest(new { message = "Invalid StartTime format. Use a valid DateTime string." });
            }

            var note = request.Note;
            if (note == null)
            {
                note = "";
            }

            var booking = new Booking
            {
                StartTime = startTime,
                Service = service,
                CreatedBy = creator,
                CreatedOn = DateTime.Now,
                UpdatedOn = DateTime.Now,
                Status = "booked",
                Note = note,
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, booking);
        }


        [HttpGet("{id}")]
        [Authorize(Policy = "user")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Service)
                .ThenInclude(bb => bb.CategoryService)
                .Include(b => b.CreatedBy)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found." });
            }

            return Ok(booking);
        }

        [HttpGet("listByUser")]
        [Authorize(Policy = "user")]
        public async Task<IActionResult> GetBookingsByUserId(int page = 1, int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);

            if (page <= 0 || pageSize <= 0)
            {
                return BadRequest(new { message = "Page and pageSize must be positive integers." });
            }

            var skip = (page - 1) * pageSize;

            var bookingsQuery = _context.Bookings
                .Include(b => b.Service)
                .ThenInclude(bb => bb.CategoryService)
                .Include(b => b.CreatedBy)
                .Where(b => b.CreatedBy.Id == userId);

            var totalCount = await bookingsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var bookings = await bookingsQuery
                .OrderByDescending(b => b.CreatedOn)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PagedResult<Booking>
            {
                Items = bookings,  
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize
            });
        }



        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateBooking(int id, [FromBody] BookingUpdateRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            if (!User.IsInRole("user") && !User.IsInRole("staff"))
            {
                return Forbid();
            }

            var booking = await _context.Bookings
                .Include(b => b.Service)
                .Include(b => b.CreatedBy)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound(new { message = "Booking not found." });
            }

            var isUser = User.IsInRole("user");
            var isStaff = User.IsInRole("staff");
                if (isUser){
                    if (!string.IsNullOrWhiteSpace(request.Status))
                    {
                        if (booking.Status == "booked" && request.Status != "rejected")
                        {
                            return BadRequest(new { message = "User can only update status from 'booked' to 'rejected'." });
                        }
                        if (booking.Status == "rejected" && request.Status != "booked")
                        {
                            return BadRequest(new { message = "User can only update status from 'rejected' to 'booked'." });
                        }

                        booking.Status = request.Status;
                        }
                }

                if (isStaff)
                {
                    if (!string.IsNullOrWhiteSpace(request.Status))
                    {

                    if (booking.Status == "booked" &&
                      !new[] { "rejected", "confirmed" }.Contains(request.Status))
                    {
                        return BadRequest(new { message = "Staff can only update status from 'booked' to 'rejected' or 'confirmed'." });
                    }
                    if (booking.Status == "rejected" && ( request.Status != "confirmed"))
                        {
                            return BadRequest(new { message = "Staff can only update status from 'rejected' to 'confirmed'." });
                        }
                        if (booking.Status == "confirmed" && (request.Status != "check-in"))
                        {
                            return BadRequest(new { message = "Staff can only update status from 'confirmed' to 'check-in'." });
                        }
                        if (booking.Status == "check-in" && (request.Status != "paid"))
                        {
                            return BadRequest(new { message = "Staff can only update status from 'check-in' to 'paid'." });
                        }
                        if (booking.Status == "paid" && !string.IsNullOrEmpty(request.Status))
                        {
                            return BadRequest(new { message = "Can't update booking status when booking status was paid" });
                        }

                    booking.Status = request.Status;
                    }


                }
            if (request.ServiceId.HasValue)
            {
                var service = await _context.Services.FindAsync(request.ServiceId.Value);
                if (service == null)
                {
                    return NotFound(new { message = "Service not found." });
                }
                booking.Service = service;
            }

            if (!string.IsNullOrWhiteSpace(request.StartTime))
            {
                if (!DateTime.TryParse(request.StartTime, out var startTime))
                {
                    return BadRequest(new { message = "Invalid StartTime format. Use a valid DateTime string." });
                }
                booking.StartTime = startTime;
            }

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                booking.Note = request.Note;
            }

            booking.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking updated successfully.", booking });
        }





        [HttpGet("listAll")]
        [Authorize(Policy = "staff")]
        public async Task<IActionResult> GetAll(
      int page = 1,
      int pageSize = 10,
      string? status = "booked",
      DateTime? startTimeFrom = null,
      DateTime? startTimeTo = null)
        {
            if (page <= 0 || pageSize <= 0)
            {
                return BadRequest(new { message = "Page and pageSize must be positive integers." });
            }

            var skip = (page - 1) * pageSize;

            var bookingsQuery = _context.Bookings
                .Include(b => b.Service)
                .ThenInclude(bb => bb.CategoryService)
                .Include(b => b.CreatedBy)
                .Where(b => b.Status == status);

            if (startTimeFrom.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.StartTime >= startTimeFrom.Value);
            }
            if (startTimeTo.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.StartTime <= startTimeTo.Value);
            }

            var totalCount = await bookingsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var bookings = await bookingsQuery
                .OrderByDescending(b => b.CreatedOn)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PagedResult<Booking>
            {
                Items = bookings,
                TotalCount = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize
            });
        }

        [HttpPost("create-multiple")]
        [Authorize(Policy = "user")]
        public async Task<IActionResult> CreateMultipleBookingsForService([FromBody] BookingCreateForServiceRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);
            var creator = await _context.Users.FindAsync(userId);
            if (creator == null)
            {
                return NotFound(new { message = "User not found." });
            }
            if (request is null)
            {
                return BadRequest(new { message = "No data input found." });
            }

            var service = await _context.Services.FindAsync(request.ServiceId);
            if (service == null)
            {
                return NotFound(new { message = "Service not found." });
            }

            foreach (var startTimeString in request.StartTimes)
            {
                if (!DateTime.TryParse(startTimeString, out var startTime))
                {
                    return BadRequest(new { message = $"Invalid StartTime format for {startTimeString}. Use a valid DateTime string." });
                }

                var existingBooking = await _context.Bookings
                    .Where(b => b.CreatedBy.Id == userId &&
                                b.Service.Id == request.ServiceId &&
                                (b.Status == "booked" || b.Status == "confirmed" || b.Status == "check-in") &&
                                b.StartTime == startTime)
                    .FirstOrDefaultAsync();

                if (existingBooking != null)
                {
                    return Conflict(new { message = $"A booking already exists with the same service and StartTime {startTimeString}." });
                }

                var booking = new Booking
                {
                    StartTime = startTime,
                    Service = service,
                    CreatedBy = creator,
                    CreatedOn = DateTime.Now,
                    UpdatedOn = DateTime.Now,
                    Status = "booked",
                    Note = request.Note ?? ""
                };

                _context.Bookings.Add(booking);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Bookings created successfully."
            });
        }

        

    }
}