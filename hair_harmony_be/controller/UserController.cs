using hair_harmony_be.hair_harmony_be.repositoty.model;
using HairSalon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;


namespace hair_harmony_be.controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// API Đăng nhập.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await GetUserIfExists(request.Password, request.UserName);

            if (user == null)
            {
                return Unauthorized("Sai tài khoản hoặc mật khẩu.");
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken(user.Id.ToString());

            return Ok(new
            {
                Message = "Đăng nhập thành công",
                Token = token,
                RefreshToken = refreshToken
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
            new Claim(ClaimTypes.Role, user.Role?.Title ?? "User")
        };

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("refresh-token")]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return BadRequest("Refresh token không được để trống.");
                }

                var principal = ValidateToken(request.RefreshToken);
                if (principal == null)
                {
                    return Unauthorized("Refresh token không hợp lệ.");
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Không thể lấy userId từ refresh token.");
                }

                var user = _context.Users.FirstOrDefault(u => u.Id.ToString() == userId);
                if (user == null)
                {
                    return Unauthorized("Người dùng không tồn tại.");
                }

                var newToken = GenerateJwtToken(user);
                var newRefreshToken = GenerateRefreshToken(userId);

                return Ok(new
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        private ClaimsPrincipal ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private string GenerateRefreshToken(string userId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

            var token = new JwtSecurityToken(
                null,
                null,
                claims,
                expires: DateTime.UtcNow.AddDays(7), // Refresh Token có hiệu lực 7 ngày
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<User> GetUserIfExists(string password, string userName)
        {
            // Lấy người dùng theo UserName trước
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == userName && u.Status == true);

            // Nếu người dùng không tồn tại hoặc mật khẩu không khớp, trả về null
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                return null;
            }

            return user;
        }


        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được người dùng.");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user == null)
            {
                return NotFound("Người dùng không tồn tại.");
            }

            var userProfile = new
            {
                user.Id,
                user.UserName,
                user.FullName,
                Gender = user.Gender ? "Male" : "Female",
                Dob = user.Dob.HasValue ? user.Dob.Value.ToString("yyyy-MM-dd") : null,
                user.Address,
                user.Email,
                Role = user.Role?.Title ?? "Unknown",
                user.Status,
                CreatedOn = user.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedOn = user.UpdatedOn.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return Ok(userProfile);
        }

        /// <summary>
        /// API tạo tài khoản người dùng với role mặc định là "User".
        /// </summary>
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == request.UserName))
            {
                return BadRequest("Tên đăng nhập đã tồn tại.");
            }

            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Title == "user");
            if (defaultRole == null)
            {
                return NotFound("Không tìm thấy role 'User'.");
            }

            var newUser = new User
            {
                UserName = request.UserName,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                Email = request.Email,
                Gender = request.Gender,
                Dob = request.Dob,
                Address = request.Address,
                Role = defaultRole,
                Status = true,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok("Tạo tài khoản người dùng thành công.");
        }

        /// <summary>
        /// API đăng ký tài khoản với role mặc định là "Staff".
        /// </summary>
        [HttpPost("register-staff")]
        [Authorize(Policy = "admin")]
        public async Task<IActionResult> RegisterStaff([FromBody] RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == request.UserName))
            {
                return BadRequest("Tên đăng nhập đã tồn tại.");
            }

            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Title == "staff");
            if (defaultRole == null)
            {
                return NotFound("Không tìm thấy role 'Staff'.");
            }

            var newStaff = new User
            {
                UserName = request.UserName,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                Email = request.Email,
                Gender = request.Gender,
                Dob = request.Dob,
                Address = request.Address,
                Role = defaultRole,
                Status = true,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };

            _context.Users.Add(newStaff);
            await _context.SaveChangesAsync();

            return Ok("Đăng ký tài khoản staff thành công.");
        }
    }

}
