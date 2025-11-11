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
        private readonly GetAvailableBeds _getAvailableBeds;
        private readonly ILogService _logService;
        public BookingController(IRepository<Bookings> bookingRepo, IRepository<Users> userRepo, IRepository<GuestHouses> ghRepo,
            IRepository<Rooms> roomRepo, IRepository<Beds> bedRepo, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor,
            GetCurrentAdmin getCurrentAdmin, EmailService emailService, GetAvailableBeds getAvailableBeds, ILogService logService)
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
            _getAvailableBeds = getAvailableBeds;
            _logService = logService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            // DTO Validation
            if (!TryValidateModel(dto))
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return Unauthorized();

            // === VALIDATE GUEST HOUSE ===
            var gh = await _ghRepo.GetByIdAsync(dto.GuestHouseId);
            if (gh == null || !gh.IsAvailable)
                return BadRequest("Guest House is not available.");

            // === VALIDATE ROOM ===
            var rm = await _roomRepo.GetByIdAsync(dto.RoomId);
            if (rm == null || rm.GuestHouseId != dto.GuestHouseId)
                return BadRequest("Invalid room.");

            var actualBedsCount = await _bedRepo.GetAll()
                .CountAsync(b => b.RoomId == dto.RoomId && b.IsActive);

            if (actualBedsCount != rm.Capacity)
                return BadRequest($"Room capacity mismatch. Expected: {rm.Capacity}, Found: {actualBedsCount} beds.");

            if (actualBedsCount == 0)
                return BadRequest("No active beds in this room.");

            // === VALIDATE BED (if provided) ===
            Beds? bd = null;
            if (dto.BedId.HasValue)
            {
                bd = await _bedRepo.GetByIdAsync(dto.BedId.Value);
                if (bd == null || bd.RoomId != dto.RoomId || !bd.IsActive)
                    return BadRequest("Bed is not available.");
            }

            if (_getAvailableBeds == null)
                return StatusCode(500, "Availability service not available.");

            // === CHECK BED AVAILABILITY ===
            var availableBeds = await _getAvailableBeds.GetAvailableBedsLogic(dto.RoomId, dto.StartDate, dto.EndDate);
            if (dto.BedId.HasValue && !availableBeds.Any(b => b.BedId == dto.BedId.Value && b.IsAvailable))
                return BadRequest("Selected bed is not available for these dates.");

            // === CREATE BOOKING ===
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


            //Log
            await _logService.LogBookingChangeAsync(
                bookingId: booking.BookingId,
                userId: userId,
                action: LogAction.Create,
                detail: $"Booking created for {user.EmpName} in Room {rm.RoomNumber}, Bed: {bd?.BedLabel ?? "N/A"}"
                );

            // === ADMIN EMAIL ===
            var adminEmails = await _context.Users
                .Where(u => u.UserRole == Role.Admin && !u.IsDeleted)
                .Select(u => u.Email)
                .ToListAsync();

            if (adminEmails.Any())
            {
                foreach (var email in adminEmails)
                {
                    try
                    {
                        await _emailService.SendNewBookingAlertToAdmin(
                            toEmail: email,
                            userName: user.EmpName,
                            guestHouse: gh.GuestHouseName,
                            room: rm.RoomNumber,
                            bed: bd?.BedLabel ?? "N/A",
                            checkIn: dto.StartDate,
                            checkOut: dto.EndDate,
                            purpose: dto.PurposeOfVisit
                        );
                    }
                    catch { }
                }
            }

            // === USER EMAIL (PENDING) ===
            try
            {
                await _emailService.SendBookingPendingEmailToUser(
                    toEmail: user.Email,
                    userName: user.EmpName,
                    guestHouse: gh.GuestHouseName,
                    room: rm.RoomNumber,
                    bed: bd?.BedLabel ?? "N/A",
                    checkIn: dto.StartDate,
                    checkOut: dto.EndDate,
                    purpose: dto.PurposeOfVisit
                );
            }
            catch { }

            return Ok(new
            {
                Message = "Booking request sent! Admin has been notified.",
                BookingId = booking.BookingId
            });
        }

        [HttpPost("available-beds")]
        [Authorize(Policy = "AdminOrGuest")]
        public async Task<IActionResult> GetAvailableBeds([FromBody] AvailableBedsRequestDto request)
        {
            if (request.GuestHouseId <= 0 || request.RoomId <= 0 || request.StartDate >= request.EndDate)
                return BadRequest("Invalid parameters.");

            var allBeds = _bedRepo.GetAll()
                .Where(b => b.RoomId == request.RoomId && b.IsActive)
                .Select(b => new { b.BedId, b.BedLabel });

            var allBedsList = await allBeds.ToListAsync();
            if (!allBedsList.Any())
                return Ok(new List<AvailableBedsDto>());

            var bookedBedIds = await _context.Bookings
                .Where(b =>
                    b.RoomId == request.RoomId &&
                    b.Status == BookingStatus.Accepted &&
                    b.BedId != null &&
                    b.StartDate < request.EndDate &&
                    b.EndDate > request.StartDate)
                .Select(b => b.BedId.Value)
                .ToListAsync();

            var availableBeds = allBedsList
                .Select(b => new AvailableBedsDto
                {
                    BedId = b.BedId,
                    BedLabel = b.BedLabel,
                    IsAvailable = !bookedBedIds.Contains(b.BedId)
                })
                .OrderBy(b => b.BedLabel)
                .ToList();

            return Ok(availableBeds);
        }
    }
}
