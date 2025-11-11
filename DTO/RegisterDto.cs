using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class RegisterDto
    {
        [Required] public string EmpName { get; set; } = null!;
        [Required] public string Email { get; set; } = null!;
        [Required] public string Password { get; set; } = null!;
    }
}
