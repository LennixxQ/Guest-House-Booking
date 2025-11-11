using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class AvailableBedsRequestDto
    {
        [Required] public int GuestHouseId { get; set; }
        [Required] public int RoomId { get; set; }
        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }
    }
}
