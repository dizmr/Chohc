using Microsoft.EntityFrameworkCore;

namespace Server.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<PriceHistory> PriceHistories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseMySql("server=localhost;database=tarkovdb;user=root;password=1234",
                new MySqlServerVersion(new Version(8, 0, 23)));
        }
    }
}