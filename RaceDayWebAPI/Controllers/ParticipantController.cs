using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.DTOs;
using RaceDayWebAPI.Models;

namespace RaceDayWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Participant")]
    public class ParticipantController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ParticipantController(RaceDayDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserDtos.ParticipantProfileResponse>> GetMe()
        {
            var participant = await _context.Participants.FindAsync(int.Parse(User.FindFirst("id")?.Value ?? "0"));
            if (participant == null)
            {
                return NotFound();
            }
            return Ok(new Participants 
            {
                ParticipantID = participant.ParticipantID,
                Name = participant.Name,
                Email = participant.Email
            });
        }

        [HttpPut("me")]
        public async Task<ActionResult<UserDtos.ParticipantProfileResponse>> UpdateMe(UserDtos.UpdateProfileRequest request)
        {
            var participant = await _context.Participants.FindAsync(int.Parse(User.FindFirst("id")?.Value ?? "0"));
            if (participant == null)
            {
                return NotFound();
            }
            if (!string.IsNullOrEmpty(request.Name))
            {
                participant.Name = request.Name;
            }
            if (!string.IsNullOrEmpty(request.Email))
            {
                participant.Email = request.Email;
            }
            if (!string.IsNullOrEmpty(request.Password))
            {
                participant.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
            await _context.SaveChangesAsync();
            return Ok(new Participants
            {
                ParticipantID = participant.ParticipantID,
                Name = participant.Name,
                Email = participant.Email
            });
        }   
    }
}
