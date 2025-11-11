using GuestHouseBookingCore.DTO;
using GuestHouseBookingCore.Models;
using GuestHouseBookingCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuestHouseBookingCore.Helpers
{
    public class GetAvailableBeds
    {
        private readonly IRepository<Beds> _bedRepo;
        private readonly ApplicationDbContext _context;

        public GetAvailableBeds(IRepository<Beds> bedRepo, ApplicationDbContext context)
        {
            _bedRepo = bedRepo ?? throw new ArgumentNullException(nameof(bedRepo));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<AvailableBedsDto>> GetAvailableBedsLogic(int roomId, DateTime startDate, DateTime endDate)
        {
            var allBeds = _bedRepo.GetAll()
            .Where(b => b.RoomId == roomId && b.IsActive)
            .Select(b => new { b.BedId, b.BedLabel });

            var allBedsList = await allBeds.ToListAsync();

            var bookedBedIds = await _context.Bookings
                .Where(b =>
                    b.RoomId == roomId &&
                    b.Status == BookingStatus.Accepted &&
                    b.BedId != null &&
                    b.StartDate < endDate &&
                    b.EndDate > startDate)
                .Select(b => b.BedId.Value)
                .ToListAsync();

            return allBedsList
                .Select(b => new AvailableBedsDto
                {
                    BedId = b.BedId,
                    BedLabel = b.BedLabel,
                    IsAvailable = !bookedBedIds.Contains(b.BedId)
                })
                .ToList();
        }
    }
}
