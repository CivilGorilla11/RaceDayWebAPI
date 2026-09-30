using Microsoft.EntityFrameworkCore;
using RaceDayWebAPI.Models;


namespace RaceDayWebAPI.Data
{
    public class RaceDayDbContext : DbContext 
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }
        
        public DbSet<Organiser> Organisers { get; set; }

        public DbSet<Participants> Participants { get; set; }


        public DbSet<MapRoute> MapRoutes { get; set; }

        public DbSet<Result> Results { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Enrollment> Enrollments { get; set; }

        public DbSet<Event> Events { get; set; }

    }
}
