using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuestHouseBookingCore.Models
{
    public enum BookingStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class Bookings
    {
        [Key]
        public int BookingId { get; set; }

        [ForeignKey(nameof(Users))]
        public int? UserId { get; set; }
        public Users? User { get; set; }

        [ForeignKey(nameof(GuestHouses))]
        public int? GuestHouseId { get; set; }
        public GuestHouses? GuestHouse { get; set; }

        [ForeignKey(nameof(Rooms))]
        public int? RoomId { get; set; }
        public Rooms? Room { get; set; }

        [ForeignKey(nameof(Beds))]
        public int? BedId { get; set; }
        public Beds? Bed { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [MaxLength(255)]
        public string PurposeOfVisit { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

        public ICollection<LogTable>? Logs { get; set; } = new List<LogTable>();
    }
}
