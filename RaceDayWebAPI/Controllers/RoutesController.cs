using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.Services;
using static RaceDayWebAPI.DTOs.CategoryDtos;

namespace RaceDayWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoutesController (RaceDayDbContext context) : ControllerBase
    {
        // POST api/category/{categoryId}/routes
        [HttpPost]
        public async Task<ActionResult<RouteResponse>> Create(int categoryId, CreateRouteRequest request )
        {
            var category = await context.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);
            if (category is null) return NotFound("Category not found.");
            if (category.Event!.OrganiserID != User.GetUserId()) return Forbid();

            var route = new Models.MapRoute
            {
                CategoryID = categoryId,
                RouteName = request.RouteName,
                ElevationGain = (decimal)request.ElevationGain,
                MapLink = request.MapLink,
            };

            context.MapRoutes.Add(route);
            await context.SaveChangesAsync();

            var response = new RouteResponse(route.RouteID, route.CategoryID, route.RouteName, route.ElevationGain, route.MapLink);
            return CreatedAtAction(nameof(Create), new { categoryId }, response);
        }


    }
}
