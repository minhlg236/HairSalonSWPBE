using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;

namespace hair_harmony_be.controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentTransactionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentTransactionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        [Authorize(Policy = "staff")]
        public async Task<IActionResult> CreatePaymentTransaction([FromBody] CreatePaymentTransactionRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);
            var creator = await _context.Users.FindAsync(userId);

            var stylist = await _context.Users.FindAsync(request.StylistId);
            if (stylist == null) return NotFound("Stylist not found");

            var booking = await _context.Bookings.FindAsync(request.BookingId);
            if (booking == null) return NotFound("Booking not found");


            var paymentTransaction = new PaymentTransaction
            {
                Note = request.Note,
                Stylist = stylist,
                Booking = booking,
                CreatedBy = creator,
                UpdatedBy = creator,

            };

            _context.PaymentTransactions.Add(paymentTransaction);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPaymentTransactionById), new { id = paymentTransaction.Id }, paymentTransaction);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "staff")]
        public async Task<IActionResult> GetPaymentTransactionById(int id)
        {
            var paymentTransaction = await _context.PaymentTransactions
                .Include(pt => pt.Stylist)
                .Include(pt => pt.Booking)
                .Include(pt => pt.CreatedBy)
                .Include(pt => pt.UpdatedBy)
                .FirstOrDefaultAsync(pt => pt.Id == id);

            if (paymentTransaction == null) return NotFound("PaymentTransaction not found");

            return Ok(paymentTransaction);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "staff")]
        public async Task<IActionResult> UpdatePaymentTransaction(int id, [FromBody] UpdatePaymentTransactionRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);
            var updater = await _context.Users.FindAsync(userId);
            if (updater == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var paymentTransaction = await _context.PaymentTransactions
                .Include(pt => pt.Stylist)
                .Include(pt => pt.Booking)
                .FirstOrDefaultAsync(pt => pt.Id == id);

            if (paymentTransaction == null) return NotFound("PaymentTransaction not found");

            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                paymentTransaction.Note = request.Note;
            }

            if (request.StylistId.HasValue)
            {
                var stylist = await _context.Users.FindAsync(request.StylistId.Value);
                if (stylist == null) return NotFound("Stylist not found");
                paymentTransaction.Stylist = stylist;
            }

            if (request.Status.HasValue)
            {
                paymentTransaction.Status = request.Status.Value;
            }

            paymentTransaction.UpdatedBy = updater;
            paymentTransaction.UpdatedOn = DateTime.Now;

            _context.PaymentTransactions.Update(paymentTransaction);
            await _context.SaveChangesAsync();

            return Ok(paymentTransaction);
        }

        [HttpGet("getAll")]
        [Authorize(Policy = "staff")]
        public async Task<IActionResult> GetAllPaymentTransactionsForStaffManager(
    [FromQuery] int? stylistId = null,
    [FromQuery] DateTime? startTimeFrom = null,
    [FromQuery] DateTime? startTimeTo = null,
    [FromQuery] int page = 1,
    [FromQuery] int size = 10,
    [FromQuery] string bookingStatus = null // Thêm filter theo Booking.Status
)
        {
            var query = _context.PaymentTransactions
                .Include(pt => pt.Stylist)
                .Include(pt => pt.Booking)
                .ThenInclude(b => b.Service)
                .ThenInclude(b => b.CategoryService)
                .Include(pt => pt.CreatedBy)
                .Include(pt => pt.UpdatedBy)
                .Where(pt => pt.Status == true);

            if (stylistId.HasValue)
            {
                query = query.Where(pt => pt.Stylist.Id == stylistId.Value);
            }

            if (startTimeFrom.HasValue && startTimeTo.HasValue)
            {
                query = query.Where(pt => pt.Booking.StartTime >= startTimeFrom.Value && pt.Booking.StartTime <= startTimeTo.Value);
            }
            else if (startTimeFrom.HasValue)
            {
                query = query.Where(pt => pt.Booking.StartTime >= startTimeFrom.Value);
            }
            else if (startTimeTo.HasValue)
            {
                query = query.Where(pt => pt.Booking.StartTime <= startTimeTo.Value);
            }

            if (!string.IsNullOrEmpty(bookingStatus))
            {
                query = query.Where(pt => pt.Booking.Status == bookingStatus);
            }

            var totalRecords = await query.CountAsync();

            var paymentTransactions = await query
                .OrderBy(pt => pt.Booking.Status == "booked" ? 1
                             : pt.Booking.Status == "confirmed" ? 2
                             : pt.Booking.Status == "check-in" ? 3
                             : pt.Booking.Status == "paid" ? 4
                             : 5)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            // Nếu không có dữ liệu, vẫn trả về danh sách rỗng
            var result = paymentTransactions.Select(pt =>
            {
                var servicePrice = pt.Booking?.Service?.Price ?? 0;
                var bookingId = pt.Booking?.Id ?? 0;

                var paymentCount = _context.PaymentTransactions
                    .Count(pt => pt.Booking.Id == bookingId);

                var totalPrice = pt.Booking?.Status == "paid"
                    ? servicePrice * 0.7 / (paymentCount > 0 ? paymentCount : 1)
                    : 0;

                return new PaymentTransactionDTO
                {
                    Id = pt.Id,
                    Note = pt.Note,
                    Stylist = pt.Stylist,
                    Booking = pt.Booking,
                    Service = pt.Booking.Service,
                    TotalPrice = totalPrice,
                };
            }).ToList();

            return Ok(new
            {
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / size),
                CurrentPage = page,
                PaymentTransactions = result ?? new List<PaymentTransactionDTO>() // Đảm bảo trả về danh sách rỗng nếu không có dữ liệu
            });
        }





        // dùng khi stylist check lịch làm của mình
        [HttpGet("filterByStylistAndService")]
        [Authorize(Policy = "stylist")]
        public async Task<IActionResult> FilterPaymentTransactions(
    [FromQuery] int? serviceId = null,
    [FromQuery] DateTime? startTimeFrom = null,
    [FromQuery] DateTime? startTimeTo = null,
    [FromQuery] int page = 1,
    [FromQuery] int size = 10
)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var stylistId = int.Parse(userIdClaim.Value);

            var query = _context.PaymentTransactions
                .Include(pt => pt.Stylist)
                .Include(pt => pt.Booking)
                .ThenInclude(b => b.Service)
                .ThenInclude(b => b.CategoryService)
                .Where(pt => pt.Stylist.Id == stylistId)
                .Where(pt => pt.Status == true);


            if (serviceId.HasValue)
            {
                query = query.Where(pt => pt.Booking.Service.Id == serviceId.Value);
            }

            if (startTimeFrom.HasValue && startTimeTo.HasValue)
            {
                query = query.Where(pt => pt.Booking.StartTime >= startTimeFrom.Value && pt.Booking.StartTime <= startTimeTo.Value);
            }
            else if (startTimeFrom.HasValue)
            {
                query = query.Where(pt => pt.Booking.StartTime >= startTimeFrom.Value);
            }
            else if (startTimeTo.HasValue)
            {
                query = query.Where(pt => pt.Booking.StartTime <= startTimeTo.Value);
            }

            query = query.OrderBy(pt => pt.Booking.Status == "booked" ? 1
                             : pt.Booking.Status == "confirmed" ? 2
                             : pt.Booking.Status == "check-in" ? 3
                             : pt.Booking.Status == "paid" ? 4
                             : 5);

            var totalRecords = await query.CountAsync();

            var paymentTransactions = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var result = paymentTransactions.Select(pt =>
            {
                var servicePrice = pt.Booking?.Service?.Price ?? 0;
                var bookingId = pt.Booking?.Id ?? 0;

                var paymentCount = _context.PaymentTransactions
                    .Count(pt => pt.Booking.Id == bookingId);

                var totalPrice = pt.Booking?.Status == "paid"
                    ? servicePrice * 0.7 / (paymentCount > 0 ? paymentCount : 1)
                    : 0;

                return new PaymentTransactionDTO
                {
                    Id = pt.Id,
                    Note = pt.Note,
                    Stylist = pt.Stylist,
                    Booking = pt.Booking,
                    Service = pt.Booking.Service,
                    TotalPrice = totalPrice
                };
            }).ToList();

            return Ok(new
            {
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / size),
                CurrentPage = page,
                PaymentTransactions = result
            });
        }




        [HttpPut("deletePayment/{id}")]     // thực chất chỉ đổi status
        [Authorize(Policy = "staff")]
        public async Task<IActionResult> UpdatePaymentTransactionStatus(int id, [FromBody] UpdatePaymentStatusRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Invalid token or user ID not found in token." });
            }

            var userId = int.Parse(userIdClaim.Value);
            var updater = await _context.Users.FindAsync(userId);
            if (updater == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var paymentTransaction = await _context.PaymentTransactions
                .Include(pt => pt.Stylist)
                .Include(pt => pt.Booking)
                .FirstOrDefaultAsync(pt => pt.Id == id);

            if (paymentTransaction == null) return NotFound("PaymentTransaction not found");

            // Chỉ cập nhật status
            if (request.Status.HasValue)
            {
                paymentTransaction.Status = request.Status.Value;
            }

            paymentTransaction.UpdatedBy = updater;
            paymentTransaction.UpdatedOn = DateTime.Now;

            _context.PaymentTransactions.Update(paymentTransaction);
            await _context.SaveChangesAsync();

            return Ok(paymentTransaction);
        }

        [HttpPost("availableTimeIndexes")]    // api này dùng để check thời gian không thể booking của 1 service ,,,
        [Authorize]
        public async Task<IActionResult> GetUnavailableTimeIndexes([FromBody] AvailableTimeRequest request)
        {
            var inputDate = request.Date.Date;

            var allStylists = await _context.Users
                .Where(u => u.Role.Title == "stylist")
                .Select(u => u.Id)
                .ToListAsync();

            var busyStylistsQuery = _context.PaymentTransactions
                .Include(pt => pt.Stylist)
                .Include(pt => pt.Booking)
                    .ThenInclude(b => b.Service)
                .Where(pt => pt.Stylist.Role.Title == "stylist" &&
                             pt.Booking.StartTime.Date == inputDate &&
                             (pt.Booking.Status == "booked" ||
                              pt.Booking.Status == "confirmed" ||
                              pt.Booking.Status == "check-in"))
                .Select(pt => new
                {
                    StylistId = pt.Stylist.Id,
                    StartTime = pt.Booking.StartTime,
                    EndTime = pt.Booking.StartTime.AddMinutes(pt.Booking.Service.TimeService.HasValue
                                ? pt.Booking.Service.TimeService.Value * 60
                                : 0)
                });

            var busyStylists = await busyStylistsQuery.ToListAsync();

            var resultIndexes = new List<int>();

            for (int i = 0; i < request.ListTime.Count; i++)
            {
                try
                {
                    var timeRange = request.ListTime[i].Split("-");
                    if (timeRange.Length != 2)
                    {
                        return BadRequest(new { error = $"Invalid time format for entry {request.ListTime[i]}" });
                    }

                    var startTime = DateTime.ParseExact($"{inputDate:yyyy-MM-dd} {timeRange[0]}",
                        "yyyy-MM-dd H:mm", null);
                    var endTime = DateTime.ParseExact($"{inputDate:yyyy-MM-dd} {timeRange[1]}",
                        "yyyy-MM-dd H:mm", null);

                    var unavailableStylistCount = busyStylists
                        .Where(b => b.StartTime < endTime && b.EndTime > startTime)
                        .Select(b => b.StylistId)
                        .Distinct()
                        .Count();

                    if (unavailableStylistCount == allStylists.Count)
                    {
                        resultIndexes.Add(i);
                    }
                }
                catch (FormatException ex)
                {
                    return BadRequest(new { error = $"Invalid time format: {ex.Message}" });
                }
            }

            return Ok(resultIndexes);
        }




    }

}
