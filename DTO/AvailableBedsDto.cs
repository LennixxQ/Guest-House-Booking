namespace GuestHouseBookingCore.DTO
{
    public class AvailableBedsDto
    {
        public int BedId { get; set; }
        public string BedLabel { get; set; } = null!;
        public bool IsAvailable { get; set; }
    }
}
