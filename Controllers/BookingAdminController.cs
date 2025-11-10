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

        public BookingAdminController(IRepository<Bookings> bookingRepo, IRepository<Users> userRepo, IRepository<GuestHouses> ghRepo,
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

        // 1. GET ALL BOOKINGS (WITH FILTER)
        [HttpGet]
        public async Task<IActionResult> GetAllBookings([FromQuery] string status = "all")
        {
            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.GuestHouse)
                .Include(b => b.Room)
                .Include(b => b.Bed)
                .AsQueryable();

            if (status.ToLower() == "pending")
                query = query.Where(b => b.Status == BookingStatus.Pending);
            else if (status.ToLower() == "accepted")
                query = query.Where(b => b.Status == BookingStatus.Accepted);
            else if (status.ToLower() == "rejected")
                query = query.Where(b => b.Status == BookingStatus.Rejected);

            var bookings = await query
                .Select(b => new
                {
                    b.BookingId,
                    UserName = b.User != null ? b.User.EmpName : "Unknown",
                    GuestHouse = b.GuestHouse != null ? b.GuestHouse.GuestHouseName : "N/A",
                    Room = b.Room != null ? b.Room.RoomNumber : "N/A",
                    Bed = b.Bed != null ? b.Bed.BedLabel : "N/A",
                    b.StartDate,
                    b.EndDate,
                    b.PurposeOfVisit,
                    b.Status,
                    b.CreatedDate,
                    b.CreatedBy,
                    b.ModifiedDate,
                    b.ModifiedBy,
                    LogCount = b.Logs.Count
                })
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            return Ok(bookings);
        }

        // 2. APPROVE BOOKING
        [HttpPut("{bookingId}/accept")]
        public async Task<IActionResult> AcceptBooking(int bookingId)
        {
            // YE LINE CHANGE KIYA — INCLUDE KIYA
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
