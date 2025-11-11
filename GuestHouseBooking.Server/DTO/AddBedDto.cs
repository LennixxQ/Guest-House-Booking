using GuestHouseBookingCore.Models;
using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class AddBedDto
    {
        [Required, MaxLength(50)] public string BedLabel { get; set; } = null!;
        public BedStatus? Status { get; set; }
        public bool? IsActive { get; set; }
    }   
}
