using System.ComponentModel.DataAnnotations;

namespace GuestHouseBookingCore.Models
{
    public enum Role
    {
        Admin,
        Guest
    }

    public class Users
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string EmpName { get; set; }

        [Required, MaxLength(30)]
        public string UserName { get; set; }

        [Required, MaxLength(50)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }
        public Role UserRole { get; set; } = Role.Guest;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public ICollection<Bookings>? Bookings { get; set; } = new List<Bookings>();

        //New Fields

        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpiry { get; set; }
    }
}
