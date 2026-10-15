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
using static RaceDayWebAPI.DTOs.UserDtos;

namespace RacedayAPI.Tests
{
    public class ParticipantControllerTest
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
        public async Task GetMe_ReturnsNotFound_WhenParticipantMissing()
        {
            // Arrange
            using var ctx = CreateContext(nameof(GetMe_ReturnsNotFound_WhenParticipantMissing));
            var controller = new ParticipantController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(1, "Participant") }
            };

            // Act
            var result = await controller.GetMe();

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetMe_ReturnsParticipant_WhenExists()
        {
            // Arrange
            using var ctx = CreateContext(nameof(GetMe_ReturnsParticipant_WhenExists));
            var participant = new Participant
            {
                ParticipantID = 3,
                Name = "Joe",
                Email = "joe@example.com",
                Password = "hash"
            };
            ctx.Participants.Add(participant);
            await ctx.SaveChangesAsync();

            var controller = new ParticipantController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(3, "Participant") }
            };

            // Act
            var result = await controller.GetMe();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<UserDtos.ParticipantProfileResponse>(ok.Value);
            Assert.Equal(participant.ParticipantID, returned.ParticipantID);
            Assert.Equal(participant.Name, returned.Name);
            Assert.Equal(participant.Email, returned.Email);
        }

        [Fact]
        public async Task UpdateMe_UpdatesFields_AndHashesPassword()
        {
            // Arrange
            using var ctx = CreateContext(nameof(UpdateMe_UpdatesFields_AndHashesPassword));
            var participant = new Participant
            {
                ParticipantID = 7,
                Name = "Before",
                Email = "before@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("oldpass")
            };
            ctx.Participants.Add(participant);
            await ctx.SaveChangesAsync();

            var controller = new ParticipantController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(7, "Participant") }
            };

            var request = new UpdateProfileRequest("After", "after@example.com", "newpass");

            // Act
            var result = await controller.UpdateMe(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<UserDtos.ParticipantProfileResponse>(ok.Value);
            Assert.Equal(7, returned.ParticipantID);
            Assert.Equal("After", returned.Name);
            Assert.Equal("after@example.com", returned.Email);

            var persisted = await ctx.Participants.FindAsync(7);
            Assert.NotNull(persisted);
            Assert.Equal("After", persisted!.Name);
            Assert.Equal("after@example.com", persisted.Email);
            Assert.True(BCrypt.Net.BCrypt.Verify("newpass", persisted.Password));
        }
    }
}
