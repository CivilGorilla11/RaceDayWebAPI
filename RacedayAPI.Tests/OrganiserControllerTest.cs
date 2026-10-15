using System;
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
using static RaceDayWebAPI.DTOs.UserDtos;

namespace RacedayAPI.Tests
{
    public class OrganiserControllerTest
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
        public async Task GetMe_ReturnsNotFound_WhenOrganiserMissing()
        {
            // Arrange
            using var ctx = CreateContext(nameof(GetMe_ReturnsNotFound_WhenOrganiserMissing));
            var controller = new OrganiserController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(1, "Organiser") }
            };

            // Act
            var result = await controller.GetMe();

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetMe_ReturnsOrganiser_WhenExists()
        {
            // Arrange
            using var ctx = CreateContext(nameof(GetMe_ReturnsOrganiser_WhenExists));
            var organiser = new Organiser
            {
                OrganiserID = 5,
                Name = "Org Name",
                Email = "org@example.com",
                Password = "hash"
            };
            ctx.Organisers.Add(organiser);
            await ctx.SaveChangesAsync();

            var controller = new OrganiserController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(5, "Organiser") }
            };

            // Act
            var result = await controller.GetMe();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<UserDtos.OrganiserProfileResponse>(ok.Value);
            Assert.Equal(organiser.OrganiserID, returned.OrganiserID);
            Assert.Equal(organiser.Name, returned.Name);
            Assert.Equal(organiser.Email, returned.Email);
        }

        [Fact]
        public async Task UpdateMe_UpdatesFields_AndHashesPassword()
        {
            // Arrange
            using var ctx = CreateContext(nameof(UpdateMe_UpdatesFields_AndHashesPassword));
            var organiser = new Organiser
            {
                OrganiserID = 9,
                Name = "Before",
                Email = "before@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("oldpass")
            };
            ctx.Organisers.Add(organiser);
            await ctx.SaveChangesAsync();

            var controller = new OrganiserController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(9, "Organiser") }
            };

            var request = new UpdateProfileRequest("After", "after@example.com", "newpass");

            // Act
            var result = await controller.UpdateMe(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<UserDtos.OrganiserProfileResponse>(ok.Value);
            Assert.Equal(9, returned.OrganiserID);
            Assert.Equal("After", returned.Name);
            Assert.Equal("after@example.com", returned.Email);

            var persisted = await ctx.Organisers.FindAsync(9);
            Assert.NotNull(persisted);
            Assert.Equal("After", persisted!.Name);
            Assert.Equal("after@example.com", persisted.Email);
            Assert.True(BCrypt.Net.BCrypt.Verify("newpass", persisted.Password));
        }
    }
}
