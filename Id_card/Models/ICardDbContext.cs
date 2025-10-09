using Microsoft.EntityFrameworkCore;

namespace Id_card.Models
{
    public class ICardDbContext : DbContext
    {
        public ICardDbContext(DbContextOptions<ICardDbContext> options)
            : base(options)
        {
        }

        public DbSet<Address> Addresses { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Employees> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Optional: specify table names explicitly
            modelBuilder.Entity<Address>().ToTable("Address");
            modelBuilder.Entity<Users>().ToTable("Users");
            modelBuilder.Entity<Employees>().ToTable("Employees");
        }
    }
}
