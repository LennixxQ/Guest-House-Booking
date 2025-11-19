using GuestHouseBookingCore.Models;

namespace GuestHouseBookingCore.Repositories
{
    public interface ILogService
    {
        Task LogBookingChangeAsync(int? bookingId, int? userId, LogAction action, string detail);

        Task LogRoomChangeAsync(LogAction action, string detail, int? userId = null, int? bookingId = null);

        Task LogGuestHouseChangeAsync(LogAction action, string detail, int? userId = null, int? bookingId = null);

    }
}
