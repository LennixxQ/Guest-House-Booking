using GuestHouseBookingCore.DTO;
using GuestHouseBookingCore.Helpers;
using GuestHouseBookingCore.Models;
using GuestHouseBookingCore.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuestHouseBookingCore.Controllers
{
    [Route("api/admin/room-master")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RoomMasterController : Controller
    {
        private readonly IRepository<GuestHouses> _ghRepo;
        private readonly IRepository<Rooms> _roomRepo;
        private readonly ApplicationDbContext _context;
        private readonly GetCurrentAdmin _getCurrentAdmin;

        public RoomMasterController(IRepository<GuestHouses> ghRepo, IRepository<Rooms> roomRepo, ApplicationDbContext context,
            GetCurrentAdmin getCurrentAdmin)
        {
            _ghRepo = ghRepo;
            _roomRepo = roomRepo;
            _context = context;
            _getCurrentAdmin = getCurrentAdmin;
        }



        [HttpPost("guesthouse/{guestHouseId}")]
        public async Task<IActionResult> AddRoom(int guestHouseId, [FromBody] AddRoomDto dto)
        {
            var gh = await _ghRepo.GetByIdAsync(guestHouseId);
            if (gh == null) return NotFound("Guest House not found");

            var room = new Rooms
            {
                GuestHouseId = guestHouseId,
                RoomNumber = dto.RoomNumber,
                Floor = dto.Floor,
                Capacity = dto.Capacity,
                Beds = new List<Beds>()
            };

            await _roomRepo.AddAsync(room);
            await _roomRepo.SaveAsync();

            return Ok(new
            {
                Message = "Room added!",
                RoomId = room.RoomId
            });
        }

        [HttpGet("guesthouse/{guestHouseId}")]
        public async Task<IActionResult> GetRooms(int guestHouseId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            var gh = await _ghRepo.GetByIdAsync(guestHouseId);
            if (gh == null) return NotFound("Guest House not found");

            var query = _context.Rooms
                .Where(r => r.GuestHouseId == guestHouseId && r.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(r => r.RoomNumber.Contains(search));

            var total = await query.CountAsync();

            var rooms = await query
                .OrderBy(r => r.RoomNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.RoomId,
                    r.RoomNumber,
                    r.Floor,
                    r.Capacity,
                    BedCount = r.Beds != null ? r.Beds.Count(b => b.IsActive) : 0,
                    r.CreatedBy,
                    r.CreatedOn
                })
                .ToListAsync();

            // YE LINE ADD KAR DE → FRONTEND KO { data, total } CHAHIYE!
            return Ok(new { data = rooms, total });
        }

        //UPDATE ROOM 
        [HttpPut("{roomId}")]
        public async Task<IActionResult> UpdateRoom(int roomId, [FromBody] UpdateRoomDto dto)
        {
            var room = await _roomRepo.GetByIdAsync(roomId);
            if (room == null) return NotFound("Room not found");

            room.RoomNumber = dto.RoomNumber ?? room.RoomNumber;
            room.Floor = dto.Floor ?? room.Floor;
            room.Capacity = dto.Capacity ?? room.Capacity;

            _roomRepo.Update(room);  // SYNC
            await _roomRepo.SaveAsync();

            return Ok(new { Message = "Room updated!" });
        }

        // DELETE ROOM — SYNC DELETE
        [HttpDelete("{roomId}")]
        public async Task<IActionResult> DeleteRoom(int roomId)
        {
            var room = await _roomRepo.GetByIdAsync(roomId);
            if (room == null) return NotFound("Room not found");

            _roomRepo.Delete(room);  // SYNC
            await _roomRepo.SaveAsync();

            return Ok(new { Message = "Room deleted!" });
        }
    }
}
