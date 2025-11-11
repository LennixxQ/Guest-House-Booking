using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GuestHouseBookingCore.Models
{
    public class GuestHouses
    {
        [Key]
        public int GuestHouseId { get; set; }
        [ForeignKey(nameof(Users))]
        public int UserId { get; set; }
        public Users User { get; set; }

        [Required, MaxLength(100)]
        public string GuestHouseName { get; set; }

        [Required, MaxLength(255)]
        public string Address { get; set; }

        [MaxLength(50)]
        public string City { get; set; }

        [MaxLength(15)]
        public string? Contact { get; set; }
        public bool IsAvailable { get; set; } = true;

        [MaxLength(50)]
        public string? CreatedBy { get; set; }

        [MaxLength(50)]
        public string? DeletedBy { get; set; }

        public ICollection<Rooms> Rooms { get; set; } = new List<Rooms>();
    }
}
