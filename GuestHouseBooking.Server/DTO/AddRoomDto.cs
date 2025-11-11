using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class AddRoomDto
    {
        [Required] public string RoomNumber { get; set; } = null!;
        [Required] public int Floor { get; set; }
        [Required] public int Capacity { get; set; }
    }
}
