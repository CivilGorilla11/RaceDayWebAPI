using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayWebAPI.Controllers;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.Models;
using RaceDayWebAPI.DTOs;
using Xunit;
using static RaceDayWebAPI.DTOs.CategoryDtos;

namespace RacedayAPI.Tests
{
    public class RoutesControllerTest
    {
        private static RaceDayDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new RaceDayDbContext(options);
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
        public async Task Create_CreatesRoute_WhenOrganiser()
        {
            // Arrange
            using var ctx = CreateContext(nameof(Create_CreatesRoute_WhenOrganiser));
            var organiserId = 400;
            var ev = new Event { OrganiserID = organiserId, EventName = "RouteEvent", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var cat = new Category { EventID = ev.EventID, CategoryName = "C", Distance = 5m, EntryFee = 10m, MaxParticipants = 50 };
            ctx.Categories.Add(cat);
            await ctx.SaveChangesAsync();

            var controller = new RoutesController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(organiserId, "Organiser") }
            };

            var request = new CreateRouteRequest("Route A", 100m, "http://map");

            // Act
            var result = await controller.Create(cat.CategoryID, request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<RouteResponse>(created.Value);
            Assert.Equal("Route A", response.RouteName);

            var persisted = await ctx.MapRoutes.FindAsync(response.RouteID);
            Assert.NotNull(persisted);
            Assert.Equal(cat.CategoryID, persisted!.CategoryID);
        }
    }
}