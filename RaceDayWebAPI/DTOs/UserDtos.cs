namespace RaceDayWebAPI.DTOs
{
    public class UserDtos
    {
        public record OrganiserProfileResponse(int OrganiserID, string Name, string Email);

        public record ParticipantProfileResponse(int ParticipantID, string Name, string Email);

        public record UpdateProfileRequest(string? Name, string? Email, string ? Password); 
    }
}
