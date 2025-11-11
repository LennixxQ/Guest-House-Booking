using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class LoginDto
    {
        [Required]
        public string EmailOrUsername { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
