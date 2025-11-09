using GuestHouseBookingCore.DTO;
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

        public RoomMasterController(IRepository<GuestHouses> ghRepo, IRepository<Rooms> roomRepo, ApplicationDbContext context)
        {
            _ghRepo = ghRepo;
            _roomRepo = roomRepo;
            _context = context;
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
        public async Task<IActionResult> GetRooms(int guestHouseId)
        {
            var gh = await _ghRepo.GetByIdAsync(guestHouseId);
            if (gh == null) return NotFound("Guest House not found");

            var rooms = await _context.Rooms
                .Where(r => r.GuestHouseId == guestHouseId)
                .Include(r => r.GuestHouse)
                .Select(r => new
                {
                    r.RoomId,
                    r.RoomNumber,
                    r.Floor,
                    r.Capacity,
                    BedCount = r.Beds != null ? r.Beds.Count : 0,
                    GuestHouse = r.GuestHouse.GuestHouseName
                })
                .ToListAsync();

            return Ok(rooms);
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
