using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Emissions.Data {
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser> {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CarbonEntry>(b => {
                b.Property(e => e.Price).HasConversion<double>();
                b.HasOne(e => e.User).WithMany().IsRequired();
            });
        }

        public DbSet<CarbonEntry> CarbonEntries { get; set; }
    }
}
