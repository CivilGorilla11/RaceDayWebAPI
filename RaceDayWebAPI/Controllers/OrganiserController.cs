using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayWebAPI.Data;
using RaceDayWebAPI.DTOs;
using BCrypt.Net;   
using Microsoft.EntityFrameworkCore;
using RaceDayWebAPI.Models;
using RaceDayWebAPI.Services;

namespace RaceDayWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Organiser")]
    public class OrganiserController(RaceDayDbContext context) : ControllerBase
    {
        [HttpGet("me")]
        public async Task<ActionResult<UserDtos.OrganiserProfileResponse>> GetMe()
        {
            var organiser = await context.Organisers.FindAsync(User.GetUserId() );

            if (organiser == null)
            {
                return NotFound();
            }
            return Ok(new Organiser
            { 

                OrganiserID = organiser.OrganiserID,
                Name = organiser.Name,
                Email = organiser.Email,
                Password = organiser.Password,
                Events = organiser.Events
            });
        }

        [HttpPut("me")]
        public async Task<ActionResult<UserDtos.OrganiserProfileResponse>>UpdateMe(UserDtos.UpdateProfileRequest request)
        {
            var organiser = await context.Organisers.FindAsync(User.GetUserId());
            if (organiser == null)
            {
                return NotFound();
            }
            if (!string.IsNullOrEmpty(request.Name))
            {
                organiser.Name = request.Name;
            }
            if (!string.IsNullOrEmpty(request.Email))
            {
                organiser.Email = request.Email;
            }
            if (!string.IsNullOrEmpty(request.Password))
            {
                organiser.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
            await context.SaveChangesAsync();
            return Ok(new Organiser
            {
                OrganiserID = organiser.OrganiserID,
                Name = organiser.Name,
                Email = organiser.Email,
                Password = organiser.Password,
                Events = organiser.Events
            });
        }



    }
}
