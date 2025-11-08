using GuestHouseBookingCore.Models;
using Microsoft.EntityFrameworkCore;

namespace GuestHouseBookingCore
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Users> Users { get; set; }
        public DbSet<GuestHouses> GuestHouses { get; set; }
        public DbSet<Rooms> Rooms { get; set; }
        public DbSet<Beds> Beds { get; set; }
        public DbSet<Bookings> Bookings { get; set; }
        public DbSet<LogTable> LogTable { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Soft Delete
            modelBuilder.Entity<Users>().HasQueryFilter(u => !u.IsDeleted);

            // === ENUMS TO STRING (Already Perfect) ===
            modelBuilder.Entity<Users>()
                .Property(u => u.UserRole)
                .HasConversion<string>();

            modelBuilder.Entity<Beds>()
                .Property(b => b.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Bookings>()
                .Property(b => b.Status)
                .HasConversion<string>();

            modelBuilder.Entity<LogTable>()
                .Property(l => l.LogAction)
                .HasConversion<string>();

            // === 1. USER DELETE → SAFE FOR BOOKINGS & GUESTHOUSES ===
            modelBuilder.Entity<Bookings>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict); // SAFE

            modelBuilder.Entity<GuestHouses>()
                .HasOne(g => g.User)
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Restrict); // SAFE

            // === 2. GUESTHOUSE DELETE → ROOMS + BEDS DELETE ===
            modelBuilder.Entity<Rooms>()
                .HasOne(r => r.GuestHouse)
                .WithMany(g => g.Rooms)
                .HasForeignKey(r => r.GuestHouseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Beds>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Beds)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // === 3. BOOKINGS → SAFE FROM ALL DELETES ===
            modelBuilder.Entity<Bookings>()
                .HasOne(b => b.Bed)
                .WithMany(bed => bed.Bookings)
                .HasForeignKey(b => b.BedId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bookings>()
                .HasOne(b => b.Room)
                .WithMany()
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bookings>()
                .HasOne(b => b.GuestHouse)
                .WithMany()
                .HasForeignKey(b => b.GuestHouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // === LOGS ===
            modelBuilder.Entity<LogTable>()
                .HasOne(l => l.Booking)
                .WithMany(b => b.Logs)
                .HasForeignKey(l => l.BookingId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            modelBuilder.Entity<LogTable>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
