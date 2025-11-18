using GuestHouseBookingCore.Models;

namespace GuestHouseBookingCore.DTO
{
    public class UpdateBedDto
    {
        public string? BedLabel { get; set; }
        public string? Status { get; set; }
        public bool? IsActive { get; set; }
    }
}
