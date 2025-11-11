using GuestHouseBookingCore.Models;
using GuestHouseBookingCore.Repositories;

namespace GuestHouseBookingCore.Services
{
    public class LogService : ILogService
    {
        private readonly IRepository<LogTable> _logRepo;

        public LogService(IRepository<LogTable> logRepo)
        {
            _logRepo = logRepo;
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
    }
}
