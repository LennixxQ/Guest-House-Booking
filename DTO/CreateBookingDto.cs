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

        // CUSTOM VALIDATION — IValidatableObject
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate >= EndDate)
                yield return new ValidationResult("Check-out must be after check-in.", new[] { nameof(EndDate) });

            if (StartDate < DateTime.Today)
                yield return new ValidationResult("Cannot book past dates.", new[] { nameof(StartDate) });

            if (StartDate > DateTime.Today.AddDays(30))
                yield return new ValidationResult("Booking allowed only up to 30 days in advance.", new[] { nameof(StartDate) });

            // MON-FRI ONLY (SUNDAY = 0)
            if (StartDate.DayOfWeek == DayOfWeek.Saturday || StartDate.DayOfWeek == DayOfWeek.Sunday)
                yield return new ValidationResult("Bookings allowed only Monday to Friday.", new[] { nameof(StartDate) });
        }
    }
}
