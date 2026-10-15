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
using static RaceDayWebAPI.DTOs.Enrollment;

namespace RacedayAPI.Tests
{
    public class EnrollmentsControllerTest
    {
        private static RaceDayDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(dbName + "-" + Guid.NewGuid().ToString("N"))
                .Options;
            var context = new RaceDayDbContext(options);
            //context.Database.OpenConnection();
            context.Database.EnsureCreated();
            return context;
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
        public async Task Create_ReturnsCreated_AndPersistsEnrollment()
        {
            // Arrange
            using var ctx = CreateContext(nameof(Create_ReturnsCreated_AndPersistsEnrollment));
            var ev = new Event { OrganiserID = 1, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var category = new Category { EventID = ev.EventID, CategoryName = "5K", Distance = 5m, EntryFee = 10m, MaxParticipants = 2 };
            ctx.Categories.Add(category);
            await ctx.SaveChangesAsync();

            var participantId = 11;
            var controller = new EnrollmentsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(participantId, "Participant") }
            };

            var request = new CreateEnrollmentRequest(category.CategoryID);

            // Act
            var result = await controller.Create(request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<EnrollmentResponse>(created.Value);
            Assert.Equal(participantId, response.ParticipantID);
            Assert.Equal(category.CategoryID, response.CategoryID);

            var persisted = ctx.Enrollments.FirstOrDefault(e => e.EnrollmentID == response.EnrollmentID);
            Assert.NotNull(persisted);
            Assert.Equal(participantId, persisted!.ParticipantID);
        }

        [Fact]
        public async Task Create_ReturnsConflict_WhenAlreadyEnrolled()
        {
            // Arrange
            using var ctx = CreateContext(nameof(Create_ReturnsConflict_WhenAlreadyEnrolled));
            var ev = new Event { OrganiserID = 1, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var category = new Category { EventID = ev.EventID, CategoryName = "5K", Distance = 5m, EntryFee = 10m, MaxParticipants = 10 };
            ctx.Categories.Add(category);
            await ctx.SaveChangesAsync();

            var participantId = 22;
            var existing = new RaceDayWebAPI.Models.Enrollment { ParticipantID = participantId, CategoryID = category.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" };
            ctx.Enrollments.Add(existing);
            await ctx.SaveChangesAsync();

            var controller = new EnrollmentsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(participantId, "Participant") }
            };

            var request = new CreateEnrollmentRequest(category.CategoryID);

            // Act
            var result = await controller.Create(request);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Contains("Already enrolled", conflict.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task Create_ReturnsConflict_WhenCategoryFull()
        {
            // Arrange
            using var ctx = CreateContext(nameof(Create_ReturnsConflict_WhenCategoryFull));
            var ev = new Event { OrganiserID = 1, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var category = new Category { EventID = ev.EventID, CategoryName = "5K", Distance = 5m, EntryFee = 10m, MaxParticipants = 1 };
            ctx.Categories.Add(category);
            await ctx.SaveChangesAsync();

            // fill the category
            ctx.Enrollments.Add(new RaceDayWebAPI.Models.Enrollment { ParticipantID = 1, CategoryID = category.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" });
            await ctx.SaveChangesAsync();

            var controller = new EnrollmentsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(2, "Participant") }
            };

            var request = new CreateEnrollmentRequest(category.CategoryID);

            // Act
            var result = await controller.Create(request);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Contains("Category is full", conflict.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task GetMine_ReturnsOnlyUserEnrollments()
        {
            // Arrange
            using var ctx = CreateContext(nameof(GetMine_ReturnsOnlyUserEnrollments));
            var ev = new Event { OrganiserID = 1, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var category = new Category { EventID = ev.EventID, CategoryName = "5K", Distance = 5m, EntryFee = 10m, MaxParticipants = 10 };
            ctx.Categories.Add(category);
            await ctx.SaveChangesAsync();

            ctx.Enrollments.AddRange(
                new RaceDayWebAPI.Models.Enrollment { ParticipantID = 100, CategoryID = category.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" },
                new RaceDayWebAPI.Models.Enrollment { ParticipantID = 101, CategoryID = category.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" }
            );
            await ctx.SaveChangesAsync();

            var controller = new EnrollmentsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(101, "Participant") }
            };

            // Act
            var result = await controller.GetMine();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var values = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<EnrollmentResponse>>(ok.Value);
            Assert.Single(values);
            Assert.All(values, v => Assert.Equal(101, v.ParticipantID));
        }

        [Fact]
        public async Task Withdraw_RemovesEnrollment_WhenOwner()
        {
            // Arrange
            using var ctx = CreateContext(nameof(Withdraw_RemovesEnrollment_WhenOwner));
            var ev = new Event { OrganiserID = 1, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var category = new Category { EventID = ev.EventID, CategoryName = "5K", Distance = 5m, EntryFee = 10m, MaxParticipants = 10 };
            ctx.Categories.Add(category);
            await ctx.SaveChangesAsync();

            var enrollment = new RaceDayWebAPI.Models.Enrollment { ParticipantID = 500, CategoryID = category.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" };
            ctx.Enrollments.Add(enrollment);
            await ctx.SaveChangesAsync();

            var controller = new EnrollmentsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(500, "Participant") }
            };

            // Act
            var result = await controller.Withdraw(enrollment.EnrollmentID);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var deleted = await ctx.Enrollments.FindAsync(enrollment.EnrollmentID);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task Withdraw_ReturnsForbid_WhenNotOwner()
        {
            // Arrange
            using var ctx = CreateContext(nameof(Withdraw_ReturnsForbid_WhenNotOwner));
            var ev = new Event { OrganiserID = 1, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var category = new Category { EventID = ev.EventID, CategoryName = "5K", Distance = 5m, EntryFee = 10m, MaxParticipants = 10 };
            ctx.Categories.Add(category);
            await ctx.SaveChangesAsync();

            var enrollment = new RaceDayWebAPI.Models.Enrollment { ParticipantID = 600, CategoryID = category.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" };
            ctx.Enrollments.Add(enrollment);
            await ctx.SaveChangesAsync();

            var controller = new EnrollmentsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(601, "Participant") } // different user
            };

            // Act
            var result = await controller.Withdraw(enrollment.EnrollmentID);

            // Assert
            Assert.IsType<ForbidResult>(result);
            var exists = await ctx.Enrollments.FindAsync(enrollment.EnrollmentID);
            Assert.NotNull(exists);
        }

        [Fact]
        public async Task GetForEvent_ReturnsEnrollments_ForOrganiser()
        {
            // Arrange
            using var ctx = CreateContext(nameof(GetForEvent_ReturnsEnrollments_ForOrganiser));
            var organiserId = 77;
            var ev = new Event { OrganiserID = organiserId, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var category = new Category { EventID = ev.EventID, CategoryName = "5K", Distance = 5m, EntryFee = 10m, MaxParticipants = 10 };
            ctx.Categories.Add(category);
            await ctx.SaveChangesAsync();

            ctx.Enrollments.Add(new RaceDayWebAPI.Models.Enrollment { ParticipantID = 700, CategoryID = category.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" });
            await ctx.SaveChangesAsync();

            var controller = new EnrollmentsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(organiserId, "Organiser") }
            };

            // Act
            var result = await controller.GetForEvent(ev.EventID);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var values = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<EnrollmentResponse>>(ok.Value);
            Assert.Single(values);
            Assert.All(values, v => Assert.Equal(700, v.ParticipantID));
        }

        [Fact]
        public async Task GetForEvent_ReturnsForbid_WhenNotOrganiser()
        {
            // Arrange
            using var ctx = CreateContext(nameof(GetForEvent_ReturnsForbid_WhenNotOrganiser));
            var organiserId = 88;
            var ev = new Event { OrganiserID = organiserId, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var category = new Category { EventID = ev.EventID, CategoryName = "5K", Distance = 5m, EntryFee = 10m, MaxParticipants = 10 };
            ctx.Categories.Add(category);
            await ctx.SaveChangesAsync();

            ctx.Enrollments.Add(new RaceDayWebAPI.Models.Enrollment { ParticipantID = 800, CategoryID = category.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" });
            await ctx.SaveChangesAsync();

            var controller = new EnrollmentsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(999, "Organiser") } // different organiser
            };

            // Act
            var result = await controller.GetForEvent(ev.EventID);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }
    }
}
