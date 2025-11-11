using GuestHouseBookingCore.Models;

namespace GuestHouseBookingCore.DTO
{
    public class UpdateUserDto
    {
        public string? EmpName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public Role? UserRole { get; set; }
    }
}
