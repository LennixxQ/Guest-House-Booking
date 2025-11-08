using GuestHouseBookingCore.DTO;
using GuestHouseBookingCore.Models;
using GuestHouseBookingCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace GuestHouseBookingCore.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Users user)
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email)) return
                    BadRequest("User with this Email Already Exists");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User registered successfully!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid Email or Password");

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                Message = "Login Successful!",
                Token = token,
                Role = user.UserRole,
                UserId = user.UserId
            });
        }

        // READ (Get User by Id) — Admin or the same User
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            //Validation
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)??"0");
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && userId != id) return Forbid("You are not allowed to access this user data.");

            //Logic
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User Not Found");

            return Ok(new
            {
                user.UserId,
                user.UserName,
                user.EmpName,
                user.Email,
                user.UserRole,
                user.CreatedAt
            });
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var userList = await _context.Users.Select(u => new
            {
                u.UserId,
                u.EmpName,
                u.UserName,
                u.Email,
                u.UserRole,
                u.CreatedAt
            }).ToListAsync();

            return Ok(userList);
       }

        // 🔹 UPDATE (only Admin can update any user)
        [HttpPut("{id}")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not Found.");

            user.UserName = dto.UserName ?? user.UserName;
            user.EmpName = dto.EmpName ?? user.EmpName;
            user.UserRole = dto.UserRole ?? user.UserRole;
            user.Email = dto.Email ?? user.Email;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "User Updated Successfully!!" });
        }

        // 🔹 DELETE (only Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u =>u.UserId == id);

            if (user == null) return NotFound("User not found.");

            if (user.IsDeleted) return BadRequest("User already deleted.");

            // SOFT DELETE
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;

            var log = new LogTable
            {
                UserId = user.UserId,
                LogType = "User",
                BookingId = null,
                LogAction = LogAction.Delete,
                LogDetail = $"[SOFT DELETE] User: {user.EmpName} ({user.Email}) marked as deleted.",
                LogDate = DateTime.UtcNow
            };

            _context.LogTable.Add(log); // Log add karo

            try
            {
            await _context.SaveChangesAsync();
            return Ok(new {
                Message = "User SOFT DELETED successfully!",
                UserId = user.UserId,
                DeletedAt = user.DeletedAt
            });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"Error: {ex.InnerException?.Message ?? ex.Message}");
            }

        }
    }
}
