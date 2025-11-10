namespace GuestHouseBookingCore.Models
{
    public class UpdateBedDto
    {
        public string? BedLabel { get; set; }
        public BedStatus? Status { get; set; }
        public bool? IsActive { get; set; }
    }
}
