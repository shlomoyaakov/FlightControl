
using Microsoft.EntityFrameworkCore;

namespace FlightControlWeb.Models
{
    public class SQLiteDBContext : DbContext
    {
        public DbSet<JsonFlightPlan> FlightPlanes { get; set; }
        
        public DbSet<Server> Servers { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=sqlite.db");
    }
}
