using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayWebAPI.Controllers;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.DTOs;
using RaceDayWebAPI.Models;
using Xunit;
using static RaceDayWebAPI.DTOs.CategoryDtos;
using static RaceDayWebAPI.DTOs.UserDtos;
using System.Security.Claims;

namespace RacedayAPI.Tests
{
    public class CategoryControllerTest
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
        public async Task GetCategoriesForEvent_ReturnsCategories()
        {
            // Arrange
            using var ctx = CreateContext(nameof(GetCategoriesForEvent_ReturnsCategories));
            var ev = new Event { OrganiserID = 1, EventName = "Test Event", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            ctx.Categories.AddRange(
                new Category { EventID = ev.EventID, CategoryName = "5K", Distance = 5m, EntryFee = 10m, MaxParticipants = 100 },
                new Category { EventID = ev.EventID, CategoryName = "10K", Distance = 10m, EntryFee = 15m, MaxParticipants = 100 }
            );
            await ctx.SaveChangesAsync();

            var controller = new CategoryController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.GetCategoriesForEvent(ev.EventID);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var values = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<CategoryResponse>>(ok.Value);
            Assert.NotEmpty(values);
            Assert.Contains(values, c => c.CategoryName == "5K");
        }

        [Fact]
        public async Task CreateCategoryForEvent_CreatesCategory_WhenOrganiser()
        {
            // Arrange
            using var ctx = CreateContext(nameof(CreateCategoryForEvent_CreatesCategory_WhenOrganiser));
            var organiserId = 42;
            var ev = new Event { OrganiserID = organiserId, EventName = "Organiser Event", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var controller = new CategoryController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal(organiserId, "Organiser")
                }
            };

            var request = new CreateCategoryRequest("Half", 21.1m, 25m, 200);

            // Act
            var result = await controller.CreateCategoryForEvent(ev.EventID, request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<CategoryResponse>(created.Value);
            Assert.Equal("Half", response.CategoryName);

            var persisted = ctx.Categories.Find(response.CategoryID);
            Assert.NotNull(persisted);
           // Assert.Equal(ev.EventID, persisted!.EventID);
        }

        [Fact]
        public async Task CreateCategoryForEvent_ReturnsNotFound_WhenNotOrganiser()
        {
            // Arrange
            using var ctx = CreateContext(nameof(CreateCategoryForEvent_ReturnsNotFound_WhenNotOrganiser));
            var ev = new Event { OrganiserID = 100, EventName = "Other Event", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var controller = new CategoryController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal(1, "Organiser") // different organiser
                }
            };

            var request = new CreateCategoryRequest("10K", 10m, 12m, 50);

            // Act
            var result = await controller.CreateCategoryForEvent(ev.EventID, request);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("Event not found", notFound.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task UpdateCategory_UpdatesValues_WhenOwner()
        {
            // Arrange
            using var ctx = CreateContext(nameof(UpdateCategory_UpdatesValues_WhenOwner));
            var organiserId = 7;
            var ev = new Event { OrganiserID = organiserId, EventName = "E", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var cat = new Category { EventID = ev.EventID, CategoryName = "Old", Distance = 5m, EntryFee = 10m, MaxParticipants = 50 };
            ctx.Categories.Add(cat);
            await ctx.SaveChangesAsync();

            var controller = new CategoryController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal(organiserId, "Organiser")
                }
            };

            var update = new UpdateCategoryRequest("NewName", 6m, 12m, 60);

            // Act
            var result = await controller.UpdateCategory(cat.CategoryID, update);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<CategoryResponse>(ok.Value);
            Assert.Equal("NewName", response.CategoryName);

            var persisted = await ctx.Categories.FindAsync(cat.CategoryID);
            Assert.Equal(6m, persisted!.Distance);
            Assert.Equal(60, persisted.MaxParticipants);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsNotFound_WhenNotOwner()
        {
            // Arrange
            using var ctx = CreateContext(nameof(UpdateCategory_ReturnsNotFound_WhenNotOwner));
            var ev = new Event { OrganiserID = 99, EventName = "E2", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var cat = new Category { EventID = ev.EventID, CategoryName = "Old", Distance = 5m, EntryFee = 10m, MaxParticipants = 50 };
            ctx.Categories.Add(cat);
            await ctx.SaveChangesAsync();

            var controller = new CategoryController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal(1, "Organiser") // different organiser
                }
            };

            var update = new UpdateCategoryRequest("NewName", null, null, null);

            // Act
            var result = await controller.UpdateCategory(cat.CategoryID, update);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("Category not found", notFound.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task DeleteCategory_RemovesCategory_WhenOwner()
        {
            // Arrange
            using var ctx = CreateContext(nameof(DeleteCategory_RemovesCategory_WhenOwner));
            var organiserId = 55;
            var ev = new Event { OrganiserID = organiserId, EventName = "ToDeleteEvent", EventDate = DateOnly.FromDateTime(DateTime.UtcNow) };
            ctx.Events.Add(ev);
            await ctx.SaveChangesAsync();

            var cat = new Category { EventID = ev.EventID, CategoryName = "Del", Distance = 3m, EntryFee = 5m, MaxParticipants = 20 };
            ctx.Categories.Add(cat);
            await ctx.SaveChangesAsync();

            var controller = new CategoryController(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUserPrincipal(organiserId, "Organiser")
                }
            };

            // Act
            var result = await controller.DeleteCategory(cat.CategoryID);

            // Assert
           Assert.IsType<NoContentResult>(result);
            var deleted = await ctx.Categories.FindAsync(cat.CategoryID);
            Assert.Null(deleted);
        }
    }
}


