using Microsoft.EntityFrameworkCore;

namespace ReservationService.Data
{
    public class ReservationDbContext : DbContext
    {
        public ReservationDbContext(DbContextOptions<ReservationDbContext> options) : base(options) { }

        public DbSet<Reservation> Reservations { get; set; } = null!;
        public DbSet<Table> Tables { get; set; } = null!;
        public DbSet<StudentProfile> StudentProfiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StudentProfile>()
                .HasIndex(p => p.StudentNumber)
                .IsUnique();
        }
    }

    public class Reservation
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public DateOnly ReservationDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsAttended { get; set; }
        public bool PenaltyProcessed { get; set; }
        public string StudentType { get; set; } = "Lisans";
    }

    public class Table
    {
        public int Id { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public int FloorId { get; set; }
    }

    public class StudentProfile
    {
        public int Id { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string StudentType { get; set; } = "Lisans";
        public int PenaltyPoints { get; set; }
        public DateOnly? BanUntil { get; set; }
        public DateTime? LastNoShowProcessedAt { get; set; }
        public string? BanReason { get; set; }
    }
}