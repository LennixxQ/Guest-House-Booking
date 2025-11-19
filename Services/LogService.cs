using GuestHouseBookingCore.Helpers;
using GuestHouseBookingCore.Models;
using GuestHouseBookingCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuestHouseBookingCore.Services
{
    public class LogService : ILogService
    {
        private readonly IRepository<LogTable> _logRepo;
        private readonly GetCurrentAdmin _getCurrentAdmin;
        private readonly ApplicationDbContext _context;

        public LogService(IRepository<LogTable> logRepo, GetCurrentAdmin currentAdmin, ApplicationDbContext context)
        {
            _logRepo = logRepo;
            _getCurrentAdmin = currentAdmin;
            _context = context;
        }
        public async Task LogBookingChangeAsync(int? bookingId, int? userId, LogAction action, string detail)
        {
            var log = new LogTable
            {
                BookingId = bookingId,
                UserId = userId,
                LogType = "Booking",
                LogAction = action,
                LogDetail = detail,
                LogDate = DateTime.UtcNow
            };

            await _logRepo.AddAsync(log);
            await _logRepo.SaveAsync();
        }

        public async Task LogRoomChangeAsync(LogAction action, string detail, int? userId = null, int? bookingId = null)
        {
            var adminId = await _getCurrentAdmin.GetCurrentAdminIdAsync();

            var log = new LogTable
            {
                BookingId = bookingId,
                UserId = adminId,
                LogType = "Room Master",
                LogAction = action,
                LogDetail = detail,
                LogDate = DateTime.UtcNow
            };

            _context.LogTable.Add(log);
            await _context.SaveChangesAsync();
        }

        // -------------------------------
        // NEW: Guest House logging
        // -------------------------------
        public async Task LogGuestHouseChangeAsync(LogAction action, string detail, int? userId = null, int? bookingId = null)
        {
            var adminId = await _getCurrentAdmin.GetCurrentAdminIdAsync();

            var log = new LogTable
            {
                BookingId = bookingId,
                UserId = adminId,
                LogType = "Guest House Master",
                LogAction = action,
                LogDetail = detail,
                LogDate = DateTime.UtcNow
            };

            _context.LogTable.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
