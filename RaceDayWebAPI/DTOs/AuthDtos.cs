namespace RaceDayWebAPI.DTOs
{
    public class AuthDtos
    {
        public record RegisterRequest(string Name, string Email, string Password);

        public record LoginRequest (string Email, string Password);


        public record AuthResponse(int Id, string Role, string Token);
    }
}
