using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuestHouseBookingCore.Models
{
    public class Rooms
    {
        [Key]
        public int RoomId { get; set; }

        [ForeignKey(nameof(GuestHouses))]
        public int GuestHouseId { get; set; }
        public GuestHouses GuestHouse { get; set; }

        [Required]
        public string RoomNumber { get; set; }
        public string? Floor { get; set; }
        public int Capacity { get; set; }
        public bool IsActive { get; set; } = true;
        public string? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }

        public ICollection<Beds>? Beds { get; set; } = new List<Beds>();
    }
}
