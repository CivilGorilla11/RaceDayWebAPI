using System;
using System.Linq;
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
using static RaceDayWebAPI.DTOs.Enrollment;

namespace RacedayAPI.Tests
{
    public class ResultsControllerTest
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
        public async Task Create_CreatesResult_WhenValid()
        {
            // Arrange
            using var ctx = CreateContext(nameof(Create_CreatesResult_WhenValid));
            var organiserId = 100;
            var ev = new Event { OrganiserID = organiserId, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var cat = new Category { EventID = ev.EventID, CategoryName = "C", Distance = 5m, EntryFee = 10m, MaxParticipants = 10 };
            ctx.Categories.Add(cat);
            await ctx.SaveChangesAsync();

            var enrol = new RaceDayWebAPI.Models.Enrollment { ParticipantID = 10, CategoryID = cat.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" };
            ctx.Enrollments.Add(enrol);
            await ctx.SaveChangesAsync();

            var controller = new ResultsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(organiserId, "Organiser") }
            };

            var request = new CreateResultRequest(enrol.EnrollmentID, TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(30)), 1, "Finished");

            // Act
            var result = await controller.Create(request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<UpdateResultResponse>(created.Value);
            Assert.Equal(enrol.EnrollmentID, response.EnrollmentID);
            var persisted = await ctx.Results.FindAsync(response.ResultId);
            Assert.NotNull(persisted);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_ForInvalidRaceStatus()
        {
            // Arrange
            using var ctx = CreateContext(nameof(Create_ReturnsBadRequest_ForInvalidRaceStatus));
            var organiserId = 110;
            var ev = new Event { OrganiserID = organiserId, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var cat = new Category { EventID = ev.EventID, CategoryName = "C", Distance = 5m, EntryFee = 10m, MaxParticipants = 10 };
            ctx.Categories.Add(cat);
            await ctx.SaveChangesAsync();

            var enrol = new RaceDayWebAPI.Models.Enrollment { ParticipantID = 11, CategoryID = cat.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" };
            ctx.Enrollments.Add(enrol);
            await ctx.SaveChangesAsync();

            var controller = new ResultsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(organiserId, "Organiser") }
            };

            var request = new CreateResultRequest(enrol.EnrollmentID, TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(35)), 2, "Running");

            // Act
            var result = await controller.Create(request);

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Race must be one of", bad.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task Update_ReturnsForbid_WhenNotOrganiser()
        {
            // Arrange
            using var ctx = CreateContext(nameof(Update_ReturnsForbid_WhenNotOrganiser));
            var organiserId = 120;
            var ev = new Event { OrganiserID = organiserId, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var cat = new Category { EventID = ev.EventID, CategoryName = "C", Distance = 5m, EntryFee = 10m, MaxParticipants = 10 };
            ctx.Categories.Add(cat);
            await ctx.SaveChangesAsync();

            var enrol = new RaceDayWebAPI.Models.Enrollment { ParticipantID = 12, CategoryID = cat.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" };
            ctx.Enrollments.Add(enrol);
            await ctx.SaveChangesAsync();

            var resultEntity = new Result { EnrollmentID = enrol.EnrollmentID, FinishTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(40)), Position = 3, Race = "Finished" };
            ctx.Results.Add(resultEntity);
            await ctx.SaveChangesAsync();

            var controller = new ResultsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(999, "Organiser") } // different organiser
            };

            var update = new UpdateResultRequest(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(39)), 2, "Finished");

            // Act
            var result = await controller.Update(resultEntity.ResultID, update);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetMine_ReturnsParticipantResults()
        {
            // Arrange
            using var ctx = CreateContext(nameof(GetMine_ReturnsParticipantResults));
            var ev = new Event { OrganiserID = 200, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var cat = new Category { EventID = ev.EventID, CategoryName = "C", Distance = 5m, EntryFee = 10m, MaxParticipants = 10 };
            ctx.Categories.Add(cat);
            await ctx.SaveChangesAsync();

            var enrol = new RaceDayWebAPI.Models.Enrollment { ParticipantID = 201, CategoryID = cat.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" };
            ctx.Enrollments.Add(enrol);
            await ctx.SaveChangesAsync();

            var res = new Result { EnrollmentID = enrol.EnrollmentID, FinishTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(50)), Position = 1, Race = "Finished" };
            ctx.Results.Add(res);
            await ctx.SaveChangesAsync();

            var controller = new ResultsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = GetUserPrincipal(201, "Participant") }
            };

            // Act
            var result = await controller.GetMine();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var values = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<UpdateResultResponse>>(ok.Value);
            Assert.Single(values);
            Assert.All(values, v => Assert.Equal(enrol.EnrollmentID, v.EnrollmentID));
        }

        [Fact]
        public async Task GetForEvent_ReturnsOrderedResults()
        {
            // Arrange
            using var ctx = CreateContext(nameof(GetForEvent_ReturnsOrderedResults));
            var organiserId = 300;
            var ev = new Event { OrganiserID = organiserId, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var cat = new Category { EventID = ev.EventID, CategoryName = "C", Distance = 5m, EntryFee = 10m, MaxParticipants = 10 };
            ctx.Categories.Add(cat);
            await ctx.SaveChangesAsync();

            var enrol1 = new RaceDayWebAPI.Models.Enrollment { ParticipantID = 301, CategoryID = cat.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" };
            var enrol2 = new RaceDayWebAPI.Models.Enrollment { ParticipantID = 302, CategoryID = cat.CategoryID, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Confirmed" };
            ctx.Enrollments.AddRange(enrol1, enrol2);
            await ctx.SaveChangesAsync();

            var r1 = new Result { EnrollmentID = enrol2.EnrollmentID, FinishTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(60)), Position = 2, Race = "Finished" };
            var r2 = new Result { EnrollmentID = enrol1.EnrollmentID, FinishTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(55)), Position = 1, Race = "Finished" };
            ctx.Results.AddRange(r1, r2);
            await ctx.SaveChangesAsync();

            var controller = new ResultsController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.GetForEvent(ev.EventID);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var values = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<UpdateResultResponse>>(ok.Value);
            var list = values.ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0].Position);
            Assert.Equal(2, list[1].Position);
        }
    }
}
