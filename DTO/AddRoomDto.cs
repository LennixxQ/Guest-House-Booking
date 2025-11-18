using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class AddRoomDto
    {
        [Required] public string RoomNumber { get; set; } = null!;
        [Required] public string? Floor { get; set; }
        [Required] public int Capacity { get; set; }
    }
}
