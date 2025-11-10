using GuestHouseBookingCore.DTO;
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

        public BedMasterController(IRepository<Rooms> roomRepo, IRepository<Beds> bedRepo, ApplicationDbContext context)
        {
            _roomRepo = roomRepo;
            _bedRepo = bedRepo;
            _context = context;
        }

        [HttpPost("room/{roomId}")]
        public async Task<IActionResult> AddBed (int roomId, [FromBody] AddBedDto dto)
        {
            var room = await _roomRepo.GetByIdAsync(roomId);
            if (room == null) return NotFound("Room not found.");

            var bed = new Beds
            {
                RoomId = roomId,
                BedLabel = dto.BedLabel,
                Status = dto.Status ?? BedStatus.Vacant,
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
                .Include(b => b.Bookings)
                .Where(b => b.RoomId == roomId)
                .Select(b => new
                {
                    b.BedId,
                    b.BedLabel,
                    b.Status,
                    b.IsActive, 
                    b.CreatedDate,
                    b.ModifiedDate,
                    BookingCount = b.Bookings.Count,
                    Room = room.RoomNumber
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
            bed.Status = dto.Status ?? bed.Status;
            bed.IsActive = dto.IsActive ?? bed.IsActive;
            bed.ModifiedDate = DateTime.UtcNow;

            _bedRepo.Update(bed);  // SYNC
            await _bedRepo.SaveAsync();

            return Ok(new { Message = "Bed updated!" });
        }

    }
}
