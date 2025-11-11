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
    [Route("api/admin/bookings")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class BookingAdminController : Controller
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
        private readonly ILogService _logService;

        public BookingAdminController(IRepository<Bookings> bookingRepo, IRepository<Users> userRepo, IRepository<GuestHouses> ghRepo,
            IRepository<Rooms> roomRepo, IRepository<Beds> bedRepo, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor,
            GetCurrentAdmin getCurrentAdmin, EmailService emailService, ILogService logService)
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
            _logService = logService;
        }

        // 1. GET ALL BOOKINGS (WITH FILTER)
        [HttpPost("create")]
        [Authorize(Roles = "Guest")]
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

            // ADMIN KO EMAIL BHEJ
            var adminEmails = await _context.Users
                .Where(u => u.UserRole == Role.Admin && u.IsDeleted == false)
                .Select(u => u.Email)
                .ToListAsync();

            foreach (var adminEmail in adminEmails)
            {
                try
                {
                    await _emailService.SendNewBookingAlertToAdmin(
                        toEmail: adminEmail,
                        userName: user.EmpName,
                        guestHouse: (await _ghRepo.GetByIdAsync(dto.GuestHouseId))?.GuestHouseName ?? "N/A",
                        room: (await _roomRepo.GetByIdAsync(dto.RoomId))?.RoomNumber ?? "N/A",
                        bed: dto.BedId.HasValue ? (await _bedRepo.GetByIdAsync(dto.BedId.Value))?.BedLabel ?? "N/A" : "N/A",
                        checkIn: dto.StartDate,
                        checkOut: dto.EndDate,
                        purpose: dto.PurposeOfVisit
                    );
                }
                catch (Exception ex)
                {
                    // LOG KAR — EMAIL FAIL HUA TO BHI BOOKING SAVE RAHE
                    Console.WriteLine($"Admin email failed: {ex.Message}");
                }
            }

            return Ok(new { Message = "Booking request sent! Admin has been notified.", BookingId = booking.BookingId });
        }

        // 2. APPROVE BOOKING
        [HttpPut("{bookingId}/accept")]
        public async Task<IActionResult> AcceptBooking(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.Bed)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound("Booking not found");
            if (booking.Status != BookingStatus.Pending)
                return BadRequest("Only pending bookings can be accepted");

            var adminName = await _getCurrentAdmin.GetCurrentAdminNameAsync();

            booking.Status = BookingStatus.Accepted;
            booking.ModifiedDate = DateTime.UtcNow;
            booking.ModifiedBy = adminName;

            if (booking.BedId.HasValue)
            {
                var bed = await _bedRepo.GetByIdAsync(booking.BedId.Value);
                if (bed != null)
                {
                    bed.Status = BedStatus.Occupied;
                    _bedRepo.Update(bed);
                }
            }

            _bookingRepo.Update(booking);
            await _bookingRepo.SaveAsync();

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            //Log
            await _logService.LogBookingChangeAsync(
                bookingId: booking.BookingId,
                userId: adminId,
                action: LogAction.Update,
                detail: $"Booking approved by Admin"
                );

            // AB USER EMAIL MIL GAYA!
            var userEmail = booking.User?.Email;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Ok(new { Message = "Booking accepted, but user email not found!", BookingId = bookingId });
            }

            try
            {
                await _emailService.SendBookingStatusEmail(
                    toEmail: userEmail,
                    userName: booking.User.EmpName,
                    status: "Accepted",
                    room: booking.Room?.RoomNumber ?? "N/A",
                    bed: booking.Bed?.BedLabel ?? "N/A",
                    checkIn: booking.StartDate,
                    checkOut: booking.EndDate,
                    adminName: adminName
                );

                return Ok(new { Message = "Booking accepted & EMAIL SENT!", EmailTo = userEmail });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Message = "Booking accepted, but EMAIL FAILED!",
                    Error = ex.Message,
                    EmailTo = userEmail
                });
            }
        }

        // 3. REJECT BOOKING WITH REASON
        [HttpPut("{bookingId}/reject")]
        public async Task<IActionResult> RejectBooking(int bookingId, [FromBody] RejectBookingDto dto)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.Bed)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound("Booking not found");
            if (booking.Status != BookingStatus.Pending)
                return BadRequest("Only pending bookings can be rejected");

            var adminName = await _getCurrentAdmin.GetCurrentAdminNameAsync();

            booking.Status = BookingStatus.Rejected;
            booking.ModifiedDate = DateTime.UtcNow;
            booking.ModifiedBy = adminName;

            _bookingRepo.Update(booking);
            await _bookingRepo.SaveAsync();

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // LOG: Booking Rejected
            await _logService.LogBookingChangeAsync(
                bookingId: booking.BookingId,
                userId: adminId,
                action: LogAction.Update,
                detail: $"Booking rejected by Admin: {dto.Reason}"
            );

            var userEmail = booking.User?.Email;
            if (!string.IsNullOrEmpty(userEmail))
            {
                await _emailService.SendBookingStatusEmail(
                    toEmail: userEmail,
                    userName: booking.User.EmpName,
                    status: "Rejected",
                    room: booking.Room?.RoomNumber ?? "N/A",
                    bed: booking.Bed?.BedLabel ?? "N/A",
                    checkIn: booking.StartDate,
                    checkOut: booking.EndDate,
                    adminName: adminName,
                    reason: dto.Reason
                );
            }
            return Ok(new { Message = "Booking rejected & email sent!", ModifiedBy = adminName, Reason = dto.Reason });
        }
    }
}
