using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = null!;

        [Required]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters")]
        public string NewPassword { get; set; } = null!;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "New password and confirm password do not match")]
        public string G { get; set; } = null!;
    }
}
