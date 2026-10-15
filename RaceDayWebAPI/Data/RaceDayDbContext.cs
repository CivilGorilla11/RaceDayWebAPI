using Microsoft.EntityFrameworkCore;
using RaceDayWebAPI.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RaceDayWebAPI.Data
{
    public class RaceDayDbContext : DbContext 
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }
        
        public DbSet<Organiser> Organisers { get; set; }

        public DbSet<Participant> Participants { get; set; }

        public DbSet<MapRoute> MapRoutes { get; set; }

        public DbSet<Result> Results { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Enrollment> Enrollments { get; set; }

        public DbSet<Event> Events { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicit primary key for MapRoute (RouteID doesn't follow MapRouteId / Id convention)
            modelBuilder.Entity<MapRoute>().HasKey(m => m.RouteID);

            // Explicit primary key for Participant (if you prefer to keep the DbSet name plural)
            modelBuilder.Entity<Participant>().HasKey(p => p.ParticipantID);

            // Add explicit keys here for any other entities that don't follow EF conventions.
        }
    }
}
