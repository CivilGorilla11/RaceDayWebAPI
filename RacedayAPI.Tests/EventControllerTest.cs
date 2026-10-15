using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayWebAPI.Controllers;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.DTOs;
using RaceDayWebAPI.Models;
using Xunit;
using static RaceDayWebAPI.DTOs.EventDtos;

namespace RacedayAPI.Tests
{
    public class EventControllerTest
    {
        private static RaceDayDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(dbName + "-" + Guid.NewGuid().ToString("N"))
                .Options;

            var ctx = new RaceDayDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        private static ClaimsPrincipal GetUserPrincipal(int id, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        [Fact]
        public async Task Search_Returns_MatchingEvents()
        {
            // Arrange
            using var ctx = CreateContext(nameof(Search_Returns_MatchingEvents));
            ctx.Events.AddRange(
                new Event { OrganiserID = 1, EventName = "City Run", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) },
                new Event { OrganiserID = 1, EventName = "Trail Walk", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) }
            );
            await ctx.SaveChangesAsync();

            var controller = new EventController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Search("Run", null, null);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var values = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<EventResponse>>(ok.Value);
            Assert.Single(values);
            Assert.Contains(values, e => e.EventName.Contains("Run"));
        }

        [Fact]
        public async Task CreateEvent_CreatesEventAndReturnsCreated()
        {
            // Arrange
            using var ctx = CreateContext(nameof(CreateEvent_CreatesEventAndReturnsCreated));
            var controller = new EventController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal(42, "Organiser")
                }
            };

            var request = new CreateEventRequest("New Race", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), "Park", "Morning race");

            // Act
            var result = await controller.CreateEvent(request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<EventResponse>(created.Value);
            Assert.Equal("New Race", response.EventName);

            // persisted
            var persisted = await ctx.Events.FindAsync(response.EventID);
            Assert.NotNull(persisted);
            Assert.Equal(42, persisted!.OrganiserID);
        }

        [Fact]
        public async Task UpdateEvent_ReturnsForbid_WhenNotOwner()
        {
            // Arrange
            using var ctx = CreateContext(nameof(UpdateEvent_ReturnsForbid_WhenNotOwner));
            var ev = new Event { OrganiserID = 2, EventName = "Owned Event", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var controller = new EventController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal(1, "Organiser") // different organiser
                }
            };

            var update = new UpdateEventRequest("Changed", null, null, null);

            // Act
            var result = await controller.UpdateEvent(ev.EventID, update);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task DeleteEvent_RemovesEvent_WhenOwner()
        {
            // Arrange
            using var ctx = CreateContext(nameof(DeleteEvent_RemovesEvent_WhenOwner));
            var ev = new Event { OrganiserID = 7, EventName = "Delete Me", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var controller = new EventController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal(7, "Organiser")
                }
            };

            // Act
            var result = await controller.DeleteEvent(ev.EventID);

            // Assert
            Assert.IsType<NoContentResult>(result);

            var deleted = await ctx.Events.FindAsync(ev.EventID);
            Assert.Null(deleted);
        }
    }
}