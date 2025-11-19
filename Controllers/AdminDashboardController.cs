using GuestHouseBookingCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GuestHouseBookingCore.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard-stats")]
        public IActionResult GetStats()
        {
            var total = _context.Bookings.Count();
            var pending = _context.Bookings.Count(b => b.Status == BookingStatus.Pending);
            var approved = _context.Bookings.Count(b => b.Status == BookingStatus.Accepted);
            var rejected = _context.Bookings.Count(b => b.Status == BookingStatus.Rejected);

            var occupancy = total > 0
                ? Math.Round((double)approved / total * 100, 1)
                : 0;

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalBookings = total,
                    pendingBookings = pending,
                    approvedBookings = approved,
                    rejectedBookings = rejected,
                    occupancyRate = occupancy
                }
            });
        }

        [HttpGet("pending-bookings")]
        public IActionResult GetPending()
        {
            var pending = _context.Bookings
                .Where(b => b.Status == BookingStatus.Pending)
                .Include(b => b.User)
                .Include(b => b.GuestHouse)
                .Include(b => b.Room)
                .Include(b => b.Bed)
                .Select(b => new
                {
                    b.BookingId,
                    UserName = b.User.UserName,
                    GuestHouseName = b.GuestHouse.GuestHouseName,
                    RoomNumber = b.Room.RoomNumber,
                    BedLabel = b.Bed.BedLabel,
                    CheckIn = b.StartDate.ToString("yyyy-MM-dd"),
                    CheckOut = b.EndDate.ToString("yyyy-MM-dd"),
                    Status = b.Status.ToString(),
                    rejectionReason = b.PurposeOfVisit
                })
                .Take(10)
                .ToList();

            return Ok(new { success = true, data = pending });
        }

        // YE HAI SABSE BADA FIX — Translation Error Gone!
        [HttpGet("monthly-trend")]
        public async Task<IActionResult> GetMonthlyTrend()
        {
            var bookingsThisYear = await _context.Bookings
                .Where(b => b.StartDate.Year == DateTime.Now.Year)
                .ToListAsync(); // ← Pehle memory me laao

            var trend = bookingsThisYear
                .GroupBy(b => b.StartDate.Month)
                .Select(g => new
                {
                    MonthNumber = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // Ab client-side me month name banao — EF ko tension nahi
            var result = trend
                .OrderBy(x => x.MonthNumber)
                .Select(x => new
                {
                    month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(x.MonthNumber),
                    count = x.Count
                })
                .ToList();

            return Ok(new { success = true, data = result });
        }
    }
}

