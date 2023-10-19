namespace Microservice1
{
    using Microservice1.Models;
    using Microsoft.EntityFrameworkCore;

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Listing> Listings { get; set; }
        public DbSet<Availability> Availabilities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Availability>()
                .HasKey(a => new { a.ListingId, a.Date });

            // Configure other entity properties as needed

            base.OnModelCreating(modelBuilder);
        }
    }

}