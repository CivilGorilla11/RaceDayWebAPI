using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.Models;
using RaceDayWebAPI.Services;
using static RaceDayWebAPI.DTOs.Enrollment;

namespace RaceDayWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrollmentsController(RaceDayDbContext context) => _context = context;
        private static EnrollmentResponse ToResponse(Enrollment en) =>
            new(en.EnrollmentID, en.ParticipantID, en.CategoryID, en.EnrollmentDate, en.Status);

        [HttpPost("api/enrollments")]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<EnrollmentResponse>> Create(CreateEnrollmentRequest request)
        {
            var category = await _context.Categories.FindAsync(request.CategoryID);
            if (category == null)
            {
                return NotFound("Category not found");
            }

            var participantId = User.GetUserId();

            bool alreadyEnrolled = await _context.Enrollments
                .AnyAsync(en => en.ParticipantID == participantId && en.CategoryID == request.CategoryID);
            if (alreadyEnrolled)
            {
                return Conflict("Already enrolled in this category");
            }

            int currentCount = await _context.Enrollments
                .CountAsync(en => en.CategoryID == request.CategoryID);
            if (currentCount >= category.MaxParticipants)
            {
                return Conflict("Category is full");
            
            }
            var enrollment = new Enrollment
            {
                ParticipantID = participantId,
                CategoryID = request.CategoryID,
                EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "Confirmed"
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMine), null, ToResponse(enrollment));
        }

        
        [HttpGet]
        [Authorize(Roles = "Participant")]
        public async Task<ActionResult<IEnumerable<EnrollmentResponse>>> GetMine()
        {
            var enrollments = await _context.Enrollments.
                Where(en => en.ParticipantID == User.GetUserId()).ToListAsync();

            return Ok(enrollments.Select(ToResponse));
        }

        [HttpDelete("api/enrollments/{enrollmentId int}")]
        [Authorize(Roles = "Participants")]
        public async Task<ActionResult> Withdraw(int enrollmentId)
        {
            var enrollment = await _context.Enrollments.FindAsync(enrollmentId);

            if (enrollment is null)
            {
                return NotFound();
            }
            if (enrollment.ParticipantID != User.GetUserId())
            {
                return Forbid();
            }

            _context.Enrollments.Remove(enrollment);
            _context.SaveChangesAsync();
            return NoContent();

        }

        [HttpGet("api/events/{eventId:int}/enrollments")]
        [Authorize(Roles = "Organiser")]
        public async Task<ActionResult<IEnumerable<EnrollmentResponse>>> GetForEvent(int eventId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev is null) return NotFound("Event not found.");
            if (ev.OrganiserID != User.GetUserId()) return Forbid();

            var enrolments = await _context.Enrollments
                .Where(en => en.Category!.EventID == eventId)
                .ToListAsync();

            return Ok(enrolments.Select(ToResponse));
        }
    }
}

