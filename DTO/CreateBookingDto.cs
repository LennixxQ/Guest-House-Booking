using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.DTO
{
    public class CreateBookingDto
    {
        [Required] public int GuestHouseId { get; set; }
        [Required] public int RoomId { get; set; }
        public int? BedId { get; set; }  // Optional
        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }
        [Required, MaxLength(255)] public string PurposeOfVisit { get; set; } = null!;
    }
}
