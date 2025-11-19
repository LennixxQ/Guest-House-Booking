using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuestHouseBookingCore.Models
{
    public enum LogAction
    {
        Create,
        Update,
        Delete
    }

    public class LogTable
    {
        [Key]
        public int AuditId { get; set; }

        [ForeignKey(nameof(Bookings))]
        public int? BookingId { get; set; }

        public Bookings? Booking { get; set; }

        [ForeignKey(nameof(Users))]
        public int? UserId { get; set; }
        public Users? User { get; set; }

        [Required, MaxLength(50)]
        public string LogType { get; set; }

        [Required]
        public LogAction LogAction { get; set; }

        [Required, MaxLength(500)]
        public string LogDetail { get; set; }

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        [MaxLength(100)]
        public string? ModifiedBy { get; set; }

        public DateTime LogDate { get; set; } = DateTime.UtcNow;

    }
}
