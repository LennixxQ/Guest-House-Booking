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

        public BookingAdminController(
            IRepository<Bookings> bookingRepo,
            IRepository<Users> userRepo,
            IRepository<GuestHouses> ghRepo,
            IRepository<Rooms> roomRepo,
            IRepository<Beds> bedRepo,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            GetCurrentAdmin getCurrentAdmin,
            EmailService emailService,
            ILogService logService)
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

        // --------------------------------------------------------------------
        // CREATE BOOKING (User Only)
        // --------------------------------------------------------------------
        [HttpPost("create")]
        [Authorize(Roles = "Guest")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
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

            // LOG ENTRY
            await _logService.LogBookingChangeAsync(
                booking.BookingId,
                userId,
                LogAction.Create,
                $"Booking Created: User [{user.EmpName}], Room [{dto.RoomId}], Bed [{dto.BedId}], Dates [{dto.StartDate:yyyy-MM-dd} → {dto.EndDate:yyyy-MM-dd}]"
            );

            // EMAIL ADMINS
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
                catch { }
            }

            return Ok(new { Message = "Booking request sent!", BookingId = booking.BookingId });
        }

        // --------------------------------------------------------------------
        // ACCEPT BOOKING
        // --------------------------------------------------------------------
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

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // LOG
            await _logService.LogBookingChangeAsync(
                booking.BookingId,
                adminId,
                LogAction.Update,
                $"Booking Approved by {adminName}"
            );

            // EMAIL USER
            var userEmail = booking.User?.Email;
            if (!string.IsNullOrEmpty(userEmail))
            {
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
                }
                catch { }
            }

            return Ok(new { Message = "Booking accepted!" });
        }

        // --------------------------------------------------------------------
        // REJECT BOOKING
        // --------------------------------------------------------------------
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

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // LOG
            await _logService.LogBookingChangeAsync(
                booking.BookingId,
                adminId,
                LogAction.Update,
                $"Booking Rejected by {adminName}: {dto.Reason}"
            );

            // EMAIL USER
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

            return Ok(new { Message = "Booking rejected!" });
        }

        // --------------------------------------------------------------------
        // PENDING BOOKINGS LIST
        // --------------------------------------------------------------------
        [HttpGet("admin/pending")]
        public async Task<IActionResult> GetPendingBookings(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var query = _bookingRepo.GetAll()
                .Where(b => b.Status == BookingStatus.Pending)
                .Include(b => b.User)
                .Include(b => b.Room)
                .Include(b => b.Bed)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(b =>
                    b.User.EmpName.ToLower().Contains(search) ||
                    b.User.Email.ToLower().Contains(search) ||
                    b.Room.RoomNumber.Contains(search)
                );
            }

            var total = await query.CountAsync();

            var bookings = await query
                .OrderByDescending(b => b.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    bookingId = b.BookingId,
                    userName = b.User.EmpName,
                    checkIn = b.StartDate.ToString("yyyy-MM-dd"),
                    checkOut = b.EndDate.ToString("yyyy-MM-dd"),
                    status = b.Status.ToString(),
                    guestHouseName = b.GuestHouse.GuestHouseName,
                    roomNumber = b.Room.RoomNumber,
                    bedLabel = b.Bed != null ? b.Bed.BedLabel : null
                })
                .ToListAsync();

            return Ok(new { data = bookings, total });
        }

        // --------------------------------------------------------------------
        // DASHBOARD STATS
        // --------------------------------------------------------------------
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalBookings = await _bookingRepo.GetAll().CountAsync();
            var pendingBookings = await _bookingRepo.GetAll().CountAsync(b => b.Status == BookingStatus.Pending);

            var totalBeds = await _bedRepo.GetAll().CountAsync(b => b.IsActive);
            var occupiedBeds = await _bookingRepo.GetAll()
                .Where(b => b.Status == BookingStatus.Accepted &&
                            b.StartDate <= DateTime.Today &&
                            b.EndDate >= DateTime.Today &&
                            b.BedId != null)
                .Select(b => b.BedId.Value)
                .Distinct()
                .CountAsync();

            var occupancyRate = totalBeds > 0 ? Math.Round((double)occupiedBeds / totalBeds * 100, 2) : 0;

            return Ok(new
            {
                data = new
                {
                    totalBookings,
                    pendingBookings,
                    occupancyRate
                }
            });
        }

        // --------------------------------------------------------------------
        // BOOKING HISTORY (READ ONLY)
        // --------------------------------------------------------------------
        [HttpGet("history")]
        public async Task<IActionResult> GetBookingHistory()
        {
            var history = await _context.Bookings
                .Include(b => b.Bed).ThenInclude(b => b!.Room).ThenInclude(r => r!.GuestHouse)
                .Include(b => b.User)
                .Where(b => b.Status == BookingStatus.Accepted || b.Status == BookingStatus.Rejected)
                .OrderByDescending(b => b.CreatedDate)
                .Select(b => new
                {
                    b.BookingId,
                    guestHouseName = b.Bed!.Room!.GuestHouse!.GuestHouseName,
                    city = b.Bed!.Room!.GuestHouse!.City,
                    roomNumber = b.Bed!.Room!.RoomNumber,
                    bedLabel = b.Bed!.BedLabel,
                    guestName = b.User!.UserName,
                    guestEmail = b.User!.Email,
                    purposeOfVisit = b.PurposeOfVisit,
                    checkInDate = b.StartDate,
                    checkOutDate = b.EndDate,
                    status = b.Status.ToString(),
                    bookedOn = b.CreatedDate
                })
                .ToListAsync();

            return Ok(history);
        }

        // --------------------------------------------------------------------
        // GET LOGS
        // --------------------------------------------------------------------
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _context.LogTable
                .Include(l => l.User)
                .Include(l => l.Booking)
                .OrderByDescending(l => l.LogDate)
                .Select(l => new
                {
                    auditId = l.AuditId,
                    bookingId = l.BookingId,
                    userName = l.User != null ? l.User.EmpName : "System",
                    action = l.LogAction.ToString(),
                    detail = l.LogDetail,
                    logDate = l.LogDate
                })
                .ToListAsync();

            return Ok(new { data = logs });
        }
    }
}
