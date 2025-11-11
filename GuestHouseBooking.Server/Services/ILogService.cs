using GuestHouseBookingCore.Models;

namespace GuestHouseBookingCore.Services
{
    public interface ILogService
    {
        Task LogBookingChangeAsync(int? bookingId, int? userId, LogAction action, string detail);
    }
}
