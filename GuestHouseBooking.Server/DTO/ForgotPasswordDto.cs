using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class ForgotPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
    }
}
