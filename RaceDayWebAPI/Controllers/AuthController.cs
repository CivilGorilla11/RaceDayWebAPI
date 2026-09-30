using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.DTOs;   
using RaceDayWebAPI.Models;
using RaceDayWebAPI.Services;
using Org.BouncyCastle.Crypto.Fpe;


namespace RaceDayWebAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(RaceDayDbContext context, TokenService tokenService) : ControllerBase
    {

        [HttpPost("register/participant")]
        public async Task<ActionResult<AuthDtos.AuthResponse>> RegisterParticipant(AuthDtos.RegisterRequest request)
        {
            if (await context.Participants.AnyAsync(p => p.Email == request.Email) || await context.Organisers.AnyAsync(o => o.Email == request.Email))
            {
                return BadRequest("Email is already registered.");
            }

            var participant = new Participants
            {
                Name = request.Name,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),

            };
            context.Participants.Add(participant);
            await context.SaveChangesAsync();

            var token = tokenService.CreateToken(participant.ParticipantID, participant.Email, "Participant");

            var response = new AuthDtos.AuthResponse(participant.ParticipantID, "Participant", token);
            return Ok(new  AuthDtos.AuthResponse(participant.ParticipantID, "Participant", token));
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthDtos.AuthResponse>> Login(AuthDtos.LoginRequest request)
        {
            var participant = await context.Participants.FirstOrDefaultAsync(p => p.Email == request.Email);
            if (participant != null && BCrypt.Net.BCrypt.Verify(request.Password, participant.Password))
            {
                var token = tokenService.CreateToken(participant.ParticipantID, participant.Email, "Participant");
                return Ok(new AuthDtos.AuthResponse(participant.ParticipantID, "Participant", token));
            }
            var organiser = await context.Organisers.FirstOrDefaultAsync(o => o.Email == request.Email);
            if (organiser != null && BCrypt.Net.BCrypt.Verify(request.Password, organiser.Password))
            {
                var token = tokenService.CreateToken(organiser.OrganiserID, organiser.Email, "Organiser");
                return Ok(new AuthDtos.AuthResponse(organiser.OrganiserID, "Organiser", token));
            }
            return Unauthorized("Invalid email or password.");
        }
    }
}
            


