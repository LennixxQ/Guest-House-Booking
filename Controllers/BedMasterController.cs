using GuestHouseBookingCore.DTO;
using GuestHouseBookingCore.Helpers;
using GuestHouseBookingCore.Models;
using GuestHouseBookingCore.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuestHouseBookingCore.Controllers
{
    [Route("api/admin/bed-master")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class BedMasterController : Controller
    {
        private readonly IRepository<Rooms> _roomRepo;
        private readonly IRepository<Beds> _bedRepo;
        private readonly ApplicationDbContext _context;
        private readonly GetCurrentAdmin _getCurrentAdmin;

        public BedMasterController(IRepository<Rooms> roomRepo, IRepository<Beds> bedRepo, ApplicationDbContext context,
            GetCurrentAdmin getCurrentAdmin)
        {
            _roomRepo = roomRepo;
            _bedRepo = bedRepo;
            _context = context;
            _getCurrentAdmin = getCurrentAdmin;
        }

        [HttpPost("room/{roomId}")]
        public async Task<IActionResult> AddBed (int roomId, [FromBody] AddBedDto dto)
        {
            var room = await _roomRepo.GetByIdAsync(roomId);
            if (room == null) return NotFound("Room not found.");

            var status = Enum.Parse<BedStatus>(dto.Status, true);

            var bed = new Beds
            {
                RoomId = roomId,
                BedLabel = dto.BedLabel,
                Status = status,
                IsActive = dto.IsActive ?? true,
                CreatedDate = DateTime.UtcNow
            };

            await _bedRepo.AddAsync(bed);
            await _bedRepo.SaveAsync();

            return Ok(new
            {
                Message = "Bed Addedd Successfully!",
                BedId = bed.BedId,
                BedLabel = bed.BedLabel,
                Status = bed.Status,
                Room = room.RoomNumber
            });
        }

        // 2. GET ALL BEDS OF A ROOM
        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetBeds(int roomId)
        {
            var room = await _roomRepo.GetByIdAsync(roomId);
            if (room == null) return NotFound("Room not found");

            var beds = await _context.Beds
                .Where(b => b.RoomId == roomId)
                .Select(b => new
                {
                    b.BedId,
                    b.BedLabel,
                    Status = b.Status.ToString(),
                    b.IsActive, 
                    b.CreatedDate,
                    b.ModifiedDate,
                    BookingCount = b.Bookings.Count,
                    RoomNumber = room.RoomNumber,
                    RoomCapacity = room.Capacity
                })
                .ToListAsync();

            return Ok(beds);
        }

        [HttpPut("{bedId}")]
        public async Task<IActionResult> UpdateBed(int bedId, [FromBody] UpdateBedDto dto)
        {
            var bed = await _bedRepo.GetByIdAsync(bedId);
            if (bed == null) return NotFound("Bed not found");

            bed.BedLabel = dto.BedLabel ?? bed.BedLabel;

            if (dto.Status != null)
                bed.Status = Enum.Parse<BedStatus>(dto.Status, true);

            bed.IsActive = dto.IsActive ?? bed.IsActive;
            bed.ModifiedDate = DateTime.UtcNow;

            _bedRepo.Update(bed);  // SYNC
            await _bedRepo.SaveAsync();

            return Ok(new { Message = "Bed updated!" });
        }

        [HttpDelete("{bedId}")]
        public async Task<IActionResult> DeleteBed(int bedId)
        {
            var bed = await _bedRepo.GetByIdAsync(bedId);
            if (bed == null)
                return NotFound("Bed not found");

            var currentAdmin = await _getCurrentAdmin.GetCurrentAdminNameAsync();
            var roomNumber = bed.Room?.RoomNumber ?? "Unknown";

            // LOG ENTRY PEHLE HI BANAA LE (kyunki bed delete hone ke baad gayab ho jayega)
            var log = new LogTable
            {
                LogType = "Bed Master",
                LogAction = LogAction.Delete,
                LogDetail = $"Bed HARD DELETED: {bed.BedLabel} (Room: {roomNumber}, BedId: {bed.BedId}) by {currentAdmin}",
                LogDate = DateTime.UtcNow
            };

            // HARD DELETE — PURA RECORD DATABASE SE HATAA DEGA
            _bedRepo.Delete(bed);
            await _bedRepo.SaveAsync();

            // Log save kar de
            _context.LogTable.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Bed permanently deleted!" });
        }

    }
}
