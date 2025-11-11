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
        public int Floor { get; set; }
        public int Capacity { get; set; }

        public ICollection<Beds>? Beds { get; set; } = new List<Beds>();
    }
}
