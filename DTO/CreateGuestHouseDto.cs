namespace GuestHouseBookingCore.DTO
{
    public class CreateGuestHouseDto
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string? Contact { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}
