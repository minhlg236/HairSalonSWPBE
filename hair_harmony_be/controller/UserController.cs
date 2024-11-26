using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;


namespace hair_harmony_be.controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration; // Thêm khai báo này

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration; // Gán giá trị trong constructor
        }

        /// <summary>
        /// API Đăng nhập.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Kiểm tra User có tồn tại không
            var user = await GetUserIfExists(request.Password, request.UserName);

            if (user == null)
            {
                return Unauthorized("Sai tài khoản hoặc mật khẩu.");
            }

            // Tạo JWT Token
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Message = "Đăng nhập thành công",
                Token = token
            });
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Role, user.Role?.Title ?? "User") // Kiểm tra nếu Role là null thì lấy giá trị mặc định là "User"
    };

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        /// <summary>
        /// API lấy tất cả người dùng.
        /// </summary>
        [HttpGet("getAll")]
        [Authorize(Policy = "admin")]  // Chỉ cần xác thực người dùng
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)  // Bao gồm thông tin Role
                .Include(u => u.LevelAccount)  // Bao gồm thông tin LevelAccount
                .ToListAsync();

            var userDtos = users.Select(user => new
            {
                user.Id,
                user.UserName,
                user.FullName,
                Gender = user.Gender ? "Male" : "Female", // Chuyển đổi giới tính
                Dob = user.Dob.HasValue ? user.Dob.Value.ToString("yyyy-MM-dd") : null, // Chuyển đổi ngày sinh nếu có
                user.Address,
                Role = user.Role?.Title ?? "Unknown", // Giá trị mặc định nếu không có Role
                Email = user?.Email ?? "Unknown", // Giá trị mặc định nếu không có Role
                LevelAccount = user.LevelAccount?.Title ?? "Unknown", // Giá trị mặc định nếu không có LevelAccount
                user.Status
            }).ToList();

            return Ok(userDtos);
        }

        private async Task<User> GetUserIfExists(string password, string userName)
        {
            // Tìm người dùng thỏa mãn các điều kiện

            return await _context.Users
                .Include(u => u.Role)  // Bao gồm thông tin Role
                .FirstOrDefaultAsync(u => u.Password == password && u.UserName == userName && u.Status == true);
        }
    }
}
