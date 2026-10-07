using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.DTOs;
using RaceDayWebAPI.Models; 
using Microsoft.EntityFrameworkCore;
using RaceDayWebAPI.Services;
using Microsoft.AspNetCore.Authorization;


namespace RaceDayWebAPI.Controllers
{
    [Route("api/[controller]")] 
    [ApiController]
    public class CategoryController (RaceDayDbContext context) : ControllerBase
    {
        private static CategoryDtos.CategoryResponse ToResponse(Category c) =>
            new(c.CategoryID, c.EventID, c.CategoryName, c.Distance, c.EntryFee, c.MaxParticipants);

        [HttpGet("api/events/{eventId:int}/categories")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CategoryDtos.CategoryResponse>>> GetCategoriesForEvent(int eventId)
        {
            var categories = await context.Categories
                .Where(c => c.EventID == eventId)
                .ToListAsync();
            return Ok(categories.Select(ToResponse));
        }

        [HttpPost("api/events/{eventId:int}/categories")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<CategoryDtos.CategoryResponse>> CreateCategoryForEvent(int eventId, CategoryDtos.CreateCategoryRequest request)
        {
            var organiserId = User.GetUserId();
            var eventEntity = await context.Events.FirstOrDefaultAsync(e => e.EventID == eventId && e.OrganiserID == organiserId);
            if (eventEntity == null)
            {
                return NotFound("Event not found or you are not the organiser of this event.");
            }
            var category = new Category
            {
                EventID = eventId,
                CategoryName = request.CategoryName,
                Distance = request.Distance,
                EntryFee = request.EntryFee,
                MaxParticipants = request.MaxParticipants
            };
            context.Categories.Add(category);
            await context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategoriesForEvent), new { eventId = eventId }, ToResponse(category));
        }

        [HttpPut("api/events/categories/{categoryId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<CategoryDtos.CategoryResponse>> UpdateCategory(int categoryId, CategoryDtos.UpdateCategoryRequest request)
        {
            var organiserId = User.GetUserId();
            var category = await context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == categoryId && c.Event.OrganiserID == organiserId);
            if (category == null)
            {
                return NotFound("Category not found or you are not the organiser of this event.");
            }
            if (!string.IsNullOrEmpty(request.CategoryName))
            {
                category.CategoryName = request.CategoryName;
            }
            if (request.Distance.HasValue)
            {
                category.Distance = request.Distance.Value;
            }
            if (request.EntryFee.HasValue)
            {
                category.EntryFee = request.EntryFee.Value;
            }
            if (request.MaxParticipants.HasValue)
            {
                category.MaxParticipants = request.MaxParticipants.Value;
            }
            await context.SaveChangesAsync();
            return Ok(ToResponse(category));
        }
        [HttpDelete("api/events/categories/{categoryId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var organiserId = User.GetUserId();
            var category = await context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == categoryId && c.Event.OrganiserID == organiserId);
            if (category == null)
            {
                return NotFound("Category not found or you are not the organiser of this event.");
            }
            context.Categories.Remove(category);
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
