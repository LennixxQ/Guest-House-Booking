using GuestHouseBookingCore.DTO;
using GuestHouseBookingCore.Models;
using GuestHouseBookingCore.Repositories;
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
        private readonly IUserRepository _userRepo;
        private readonly IRepository<LogTable> _logRepo;
        private readonly RegisterService _registerService;
        private readonly EmailService _emailService;

        public AuthController(ApplicationDbContext context, JwtService jwtService, IUserRepository userRepo, 
            IRepository<LogTable> logRepo, RegisterService registerService, EmailService emailService)
        {
            _context = context;
            _jwtService = jwtService;
            _userRepo = userRepo;
            _logRepo = logRepo;
            _registerService = registerService;
            _emailService = emailService;

        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _userRepo.GetByEmailOrUsernameAsync(dto.Email) != null)
                return BadRequest("Email already exists");

            string username;
            do
            {
                username = _registerService.GenerateUsername(dto.EmpName);
            } while (await _userRepo.GetByEmailOrUsernameAsync(username) != null);

            var tempPassword = dto.Password;
            var user = new Users
            {
                EmpName = dto.EmpName,
                UserName = username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveAsync();

            // EMAIL SENT
            await _emailService.SendWelcomeEmail(
                toEmail: dto.Email,
                username: username,
                tempPassword: tempPassword
            );

            return Ok(new
            {
                Message = "User registered! Auto username generated! + Email Sent",
                UserName = username
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userRepo.GetByEmailOrUsernameAsync(dto.EmailOrUsername);

            if (user == null || user.IsDeleted)
                return Unauthorized("Invalid credentials");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                Message = "Login Successful!",
                Token = token,
                User = new
                {
                    user.UserId,
                    user.UserName,
                    user.EmpName,
                    user.Email,
                    Role = user.UserRole.ToString()
                }
            });
        }

        // READ (Get User by Id) — Admin or the same User
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && userId != id)
                return Forbid("Access denied");

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null || user.IsDeleted)
                return NotFound("User not found");

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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            if (user.IsDeleted) return BadRequest("Already deleted");

            // SOFT DELETE
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            _userRepo.Update(user);

            // LOG ADD KAR → AB _logRepo SE!
            var log = new LogTable
            {
                UserId = user.UserId,
                BookingId = null,
                LogType = "User",
                LogAction = LogAction.Delete,
                LogDetail = $"[SOFT DELETE] {user.EmpName} ({user.Email}) at {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                LogDate = DateTime.UtcNow
            };

            await _logRepo.AddAsync(log);      // AB YE CHALEGA!
            await _logRepo.SaveAsync();        // YE BHI!

            return Ok(new
            {
                Message = "User SOFT DELETED + LOGGED!",
                UserId = id
            });
        }
    }
}
