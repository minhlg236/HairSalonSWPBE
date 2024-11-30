using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<IActionResult> GetAllPaymentTransactions(
    [FromQuery] bool? status = true,          // Lọc theo status (mặc định là true)
    [FromQuery] int? stylistId = null,        // Lọc theo stylistId (nếu có)
    [FromQuery] DateTime? startTimeFrom = null, // Lọc theo starttime bắt đầu của booking (nếu có)
    [FromQuery] DateTime? startTimeTo = null,   // Lọc theo starttime kết thúc của booking (nếu có)
    [FromQuery] int page = 1,                 // Số trang (mặc định là 1)
    [FromQuery] int size = 10                 // Số bản ghi mỗi trang (mặc định là 10)
)
        {
            // Bắt đầu truy vấn từ PaymentTransactions
            var query = _context.PaymentTransactions
                .Include(pt => pt.Stylist)
                .Include(pt => pt.Booking)
                .Include(pt => pt.CreatedBy)
                .Include(pt => pt.UpdatedBy)
                .Where(pt => pt.Status == status);  // Lọc theo status (mặc định true)

            // Lọc theo stylistId nếu có
            if (stylistId.HasValue)
            {
                query = query.Where(pt => pt.Stylist.Id == stylistId.Value);
            }

            // Lọc theo starttime của booking nếu có
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

            // Lấy tổng số lượng bản ghi
            var totalRecords = await query.CountAsync();

            // Phân trang
            var paymentTransactions = await query
                .Skip((page - 1) * size)  // Bỏ qua các bản ghi trước đó
                .Take(size)               // Lấy số bản ghi theo kích thước trang
                .ToListAsync();

            // Kiểm tra nếu không có kết quả
            if (paymentTransactions == null || !paymentTransactions.Any())
            {
                return NotFound("No payment transactions found.");
            }

            // Trả về kết quả cùng thông tin phân trang
            return Ok(new
            {
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / size),
                CurrentPage = page,
                PaymentTransactions = paymentTransactions
            });
        }



    }

}
