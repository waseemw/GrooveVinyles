using Microsoft.EntityFrameworkCore;

namespace GrooveVinyles.DataAccess
{
    public class StoreDbContext : DbContext
    {
        public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options)
        {
        }

        public DbSet<Vinyl> Vinyl { get; set; }
        public DbSet<Artist> Artist { get; set; }

        public DbSet<Order> Order { get; set; }
        public DbSet<VinylPurchase> VinylPurchase { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Artist>().HasMany(c => c.Vinyles).WithOne(e => e.Artist);
            modelBuilder.Entity<Order>().HasMany(c => c.VinylPurchases).WithMany(v=>v.Orders);
            modelBuilder.Entity<VinylPurchase>().HasOne(v => v.Vinyl).WithMany(c => c.VinylPurchases);

        }
    }
}