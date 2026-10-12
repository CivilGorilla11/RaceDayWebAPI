using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = "Organiser")]
    public class RoutesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public RoutesController(RaceDayDbContext context)
        {
            _context = context;
        }

        // POST api/category/{categoryId}/routes
        [HttpPost]
        public async Task<ActionResult<RouteResponse>> Create(int categoryId, CreateRouteRequest request )
        {
            var category = await _context.Categories.Include(c => c.Event)
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

            _context.MapRoutes.Add(route);
            await _context.SaveChangesAsync();

            var response = new RouteResponse(route.RouteID, route.CategoryID, route.RouteName, route.ElevationGain, route.MapLink);
            return CreatedAtAction(nameof(Create), new { categoryId }, response);
        }


    }
}
