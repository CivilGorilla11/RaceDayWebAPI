using System;
using System.Security.Claims;
namespace RaceDayWebAPI.Services
{
    public static class ClaimsPrincipalExtensions
    {
        //Pulls the user own ID , ID must be stored in the token as a claim.
        public static int GetUserId(this ClaimsPrincipal user) =>
            int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID claim not found."));      
        }
    }


