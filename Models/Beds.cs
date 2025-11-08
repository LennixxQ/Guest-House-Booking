using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuestHouseBookingCore.Models
{
    public enum BedStatus
    {
        Occupied,
        Vacant
    }

    public class Beds
    {
        [Key]
        public int BedId { get; set; }

        [ForeignKey(nameof(Rooms))]
        public int RoomId { get; set; }
        public Rooms Room { get; set; }

        [Required, MaxLength(50)]
        public string BedLabel { get; set; }

        public BedStatus Status { get; set; } = BedStatus.Vacant;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        public ICollection<Bookings>? Bookings { get; set; } = new List<Bookings>();
    }
}
