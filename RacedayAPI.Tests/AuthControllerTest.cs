using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayWebAPI.Controllers;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.Models;
using RaceDayWebAPI.DTOs;
using RaceDayWebAPI.Services;
using Xunit;
using System.Threading.Tasks;

namespace RacedayAPI.Tests
{
    public class AuthControllerTest
    {
        private static RaceDayDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(dbName) 
                .Options;
            return new RaceDayDbContext(options);
        }

        private static TokenService CreateTokenService()
        {
            // Use Jwt:Key (matches Program.cs and TokenService)
            var inMemory = new Dictionary<string, string?> { ["Jwt:Key"] = "test-token-key-which-is-long-enough-for-tests" };
            var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
            return new TokenService(config);
        }

        [Fact]
        public async Task RegisterParticipant_CreatesUserAndReturnsToken()
        {
            // Arrange
            using var ctx = CreateContext(nameof(RegisterParticipant_CreatesUserAndReturnsToken));
            var tokenService = CreateTokenService();
            var controller = new AuthController(ctx, tokenService);

            var request = new AuthDtos.RegisterRequest("Alice", "alice@example.com", "pass123");

            // Act
            var result = await controller.RegisterParticipant(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthDtos.AuthResponse>(ok.Value);
            Assert.Equal("Participant", response.Role);
            Assert.False(string.IsNullOrEmpty(response.Token));

            var persisted = await ctx.Participants.FirstOrDefaultAsync(p => p.Email == "alice@example.com");
            Assert.NotNull(persisted);
        }

        [Fact]
        public async Task Login_ReturnsParticipantToken_WhenCredentialsValid()
        {
            // Arrange
            using var ctx = CreateContext(nameof(Login_ReturnsParticipantToken_WhenCredentialsValid));
            var plain = "secret";
            var participant = new Participant { Name = "P", Email = "p@example.com", Password = BCrypt.Net.BCrypt.HashPassword(plain) };
            ctx.Participants.Add(participant);
            await ctx.SaveChangesAsync();

            var tokenService = CreateTokenService();
            var controller = new AuthController(ctx, tokenService);
            var request = new AuthDtos.LoginRequest("p@example.com", plain);

            // Act
            var result = await controller.Login(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<AuthDtos.AuthResponse>(ok.Value);
            Assert.Equal("Participant", response.Role);
            Assert.False(string.IsNullOrEmpty(response.Token));
        }
    }
}
