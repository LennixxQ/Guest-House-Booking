using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class ResetPasswordDto
    {
        [Required] public string Token { get; set; } = null!;
        [Required, MinLength(6)] public string NewPassword { get; set; } = null!;
        [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = null!;
    }
}
