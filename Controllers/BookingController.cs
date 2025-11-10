using GuestHouseBookingCore.DTO;
using GuestHouseBookingCore.Helpers;
using GuestHouseBookingCore.Models;
using GuestHouseBookingCore.Repositories;
using GuestHouseBookingCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GuestHouseBookingCore.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    [Authorize(Policy = "AdminOrGuest")]
    public class BookingController : Controller
    {
        private readonly IRepository<Bookings> _bookingRepo;
        private readonly IRepository<Users> _userRepo;
        private readonly IRepository<GuestHouses> _ghRepo;
        private readonly IRepository<Rooms> _roomRepo;
        private readonly IRepository<Beds> _bedRepo;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GetCurrentAdmin _getCurrentAdmin;
        private readonly EmailService _emailService;
        public BookingController(IRepository<Bookings> bookingRepo, IRepository<Users> userRepo, IRepository<GuestHouses> ghRepo,
            IRepository<Rooms> roomRepo, IRepository<Beds> bedRepo, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor,
            GetCurrentAdmin getCurrentAdmin, EmailService emailService)
        {
            _bookingRepo = bookingRepo;
            _userRepo = userRepo;
            _ghRepo = ghRepo;
            _roomRepo = roomRepo;
            _bedRepo = bedRepo;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _getCurrentAdmin = getCurrentAdmin;
            _emailService = emailService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return Unauthorized();

            var booking = new Bookings
            {
                UserId = userId,
                GuestHouseId = dto.GuestHouseId,
                RoomId = dto.RoomId,
                BedId = dto.BedId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                PurposeOfVisit = dto.PurposeOfVisit,
                Status = BookingStatus.Pending,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = user.EmpName
            };

            await _bookingRepo.AddAsync(booking);
            await _bookingRepo.SaveAsync();

            // Notify all active admins
            var adminEmails = await _context.Users
                .Where(u => u.UserRole == Role.Admin && !u.IsDeleted)
                .Select(u => u.Email)
                .ToListAsync();

            if (adminEmails.Any())
            {
                var guestHouse = await _ghRepo.GetByIdAsync(dto.GuestHouseId);
                var room = await _roomRepo.GetByIdAsync(dto.RoomId);
                var bed = dto.BedId.HasValue
                    ? await _bedRepo.GetByIdAsync(dto.BedId.Value)
                    : null;

                foreach (var email in adminEmails)
                {
                    try
                    {
                        await _emailService.SendNewBookingAlertToAdmin(
                            toEmail: email,
                            userName: user.EmpName,
                            guestHouse: guestHouse?.GuestHouseName ?? "N/A",
                            room: room?.RoomNumber ?? "N/A",
                            bed: bed?.BedLabel ?? "N/A",
                            checkIn: dto.StartDate,
                            checkOut: dto.EndDate,
                            purpose: dto.PurposeOfVisit
                        );
                    }
                    catch { /* Silent fail — booking already saved */ }
                }
            }

            return Ok(new
            {
                Message = "Booking request sent! Admin has been notified.",
                BookingId = booking.BookingId
            });
        }
    }
}
