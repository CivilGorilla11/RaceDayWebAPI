using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.Models;
using RaceDayWebAPI.Services;
using static RaceDayWebAPI.DTOs.Enrollment;
using Microsoft.EntityFrameworkCore;
using RaceDayWebAPI.DTOs;

namespace RaceDayWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController(RaceDayDbContext context) : ControllerBase
    {
        private static readonly string[] ValidRaceStatuses = ["Finished", "DNF", "DQ"];

        private static UpdateResultResponse ToResponse(Result r) =>
            new(r.ResultID, r.EnrollmentID, r.FinishTime, r.Position, r.Race);

        // POST /api/results
        [HttpPost("api/results")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<UpdateResultResponse>> Create(CreateResultRequest request)
        {
            if (!ValidRaceStatuses.Contains(request.Race))
                return BadRequest($"Race must be one of: {string.Join(", ", ValidRaceStatuses)}");

            var enrolment = await context.Enrollments
                .Include(en => en.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(en => en.EnrollmentID == request.EnrollmentID);
            if (enrolment is null) return NotFound("Enrolment not found.");
            if (enrolment.Category!.Event!.OrganiserID != User.GetUserId()) return Forbid();

            var result = new Result
            {
                EnrollmentID = request.EnrollmentID,
                FinishTime = request.FinishTime,
                Position = (int)request.Position,
                Race = request.Race,
            };

            context.Results.Add(result);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMine), null, ToResponse(result));
        }

        // PUT /api/results/{resultId}
        [HttpPut("api/results/{resultId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<UpdateResultResponse>> Update(int resultId, UpdateResultRequest request)
        {
            var result = await context.Results
                .Include(r => r.Enrollment).ThenInclude(en => en!.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.ResultID == resultId);
            if (result is null) return NotFound();
            if (result.Enrollment!.Category!.Event!.OrganiserID != User.GetUserId()) return Forbid();

            if (request.FinishTime is not null) result.FinishTime = request.FinishTime.Value;
            if (request.Position is not null) result.Position = (int)request.Position;
            if (request.Race is not null)
            {
                if (!ValidRaceStatuses.Contains(request.Race))
                    return BadRequest($"Race must be one of: {string.Join(", ", ValidRaceStatuses)}");
                result.Race = request.Race;
            }

            await context.SaveChangesAsync();
            return Ok(ToResponse(result));
        }

        // DELETE /api/results/{resultId}
        [HttpDelete("api/results/{resultId:int}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> Delete(int resultId)
        {
            var result = await context.Results
                .Include(r => r.Enrollment).ThenInclude(en => en!.Category).ThenInclude(c => c!.Event)
                .FirstOrDefaultAsync(r => r.ResultID == resultId);
            if (result is null) return NotFound();
            if (result.Enrollment!.Category!.Event!.OrganiserID != User.GetUserId()) return Forbid();

            context.Results.Remove(result);
            await context.SaveChangesAsync();
            return NoContent();
        }

        // GET /api/results/me
        [HttpGet("api/results/me")]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<IEnumerable<UpdateResultResponse>>> GetMine()
        {
            var results = await context.Results
                .Where(r => r.Enrollment!.ParticipantID == User.GetUserId())
                .ToListAsync();

            return Ok(results.Select(ToResponse));
        }

        // GET /api/events/{eventId}/results
        [HttpGet("api/events/{eventId:int}/results")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<UpdateResultResponse>>> GetForEvent(int eventId)
        {
            if (!await context.Events.AnyAsync(e => e.EventID == eventId)) return NotFound("Event not found.");

            var results = await context.Results
                .Where(r => r.Enrollment!.Category!.EventID == eventId)
                .OrderBy(r => r.Position)
                .ToListAsync();

            return Ok(results.Select(ToResponse));
        }
    }

}

