using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.DTOs;
using RaceDayWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using static RaceDayWebAPI.DTOs.EventDtos;
using RaceDayWebAPI.Services;

namespace RaceDayWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class EventController(RaceDayDbContext context) : ControllerBase
    {
        public static EventResponse ToResponse(Event e) =>

        new(e.EventID, e.OrganiserID, e.EventName, e.EventDate, e.EventAddress, e.Description);




        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<EventResponse>>> Search(
            [FromQuery] string? search, [FromQuery] string? location, [FromQuery] DateOnly? date)
        {
            var query = context.Events.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.EventName.Contains(search));
            }
            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(e => e.EventAddress.Contains(location));
            }
            if (date.HasValue)
            {
                query = query.Where(e => e.EventDate == date.Value);
            }

            var events = await query.ToListAsync();
            return Ok(events.Select(ToResponse));
        }
        [HttpGet("organiser/ me ")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<IEnumerable<EventResponse>>> GetMyEvents()
        {
            var events = await context.Events.
                Where(e => e.OrganiserID == User.GetUserId()).
                OrderBy(e => e.EventDate).ToListAsync();

            return Ok(events.Select(ToResponse));
        }

        [HttpGet("{eventId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<EventResponse>> GetEvent(int eventId)
        {
            var ev = await context.Events.FindAsync(eventId);
            if (ev == null)
            {
                return NotFound();
            }
            return Ok(ToResponse(ev));
        }

        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<EventResponse>> CreateEvent(CreateEventRequest request)
        {
            var ev = new Event
            {
                OrganiserID = User.GetUserId(),
                EventName = request.EventName,
                EventDate = request.EventDate,
                EventAddress = request.EventAddress,
                Description = request.Description
            };
            context.Events.Add(ev);
            await context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetEvent), new { eventId = ev.EventID }, ToResponse(ev));

        }

        [HttpPut("{eventId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<EventResponse>> UpdateEvent(int eventId, UpdateEventRequest request)
        {
            var ev = await context.Events.FindAsync(eventId);
            if (ev == null)
            {
                return NotFound();
            }
            if (ev.OrganiserID != User.GetUserId())
            {
                return Forbid();
            }
            if (!string.IsNullOrEmpty(request.EventName))
            {
                ev.EventName = request.EventName;
            }
            ev.EventDate = request.EventDate;
            if (!string.IsNullOrEmpty(request.EventAddress))
            {
                ev.EventAddress = request.EventAddress;
            }
            if (!string.IsNullOrEmpty(request.Description))
            {
                ev.Description = request.Description;
            }
            await context.SaveChangesAsync();
            return Ok(ToResponse(ev));
        }

        [HttpDelete("{eventId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEvent(int eventId)
        {
            var ev = await context.Events.FindAsync(eventId);
            if (ev == null)
            {
                return NotFound();
            }
            if (ev.OrganiserID != User.GetUserId())
            {
                return Forbid();
            }
            context.Events.Remove(ev);
            await context.SaveChangesAsync();
            return NoContent();
        }

    }

}
