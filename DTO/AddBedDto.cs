using GuestHouseBookingCore.Models;
using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class AddBedDto
    {
        [Required, MaxLength(50)] public string BedLabel { get; set; } = null!;
        public string Status { get; set; } = "Vacant";
        public bool? IsActive { get; set; } = true;
    }   
}
