using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class RejectBookingDto
    {
        [Required] public string Reason { get; set; } = null!;
    }
}
