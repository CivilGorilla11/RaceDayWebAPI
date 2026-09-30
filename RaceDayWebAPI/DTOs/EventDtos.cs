namespace RaceDayWebAPI.DTOs
{
    public class EventDtos
    {
        public record CreateEventRequest(string EventName, DateOnly EventDate, string? EventAddress, string ? Description);

        public record UpdateEventRequest(string ? EventName, DateOnly EventDate, string ? EventAddress, string? Description);

        public record EventResponse(
           int EventID,
           int OrganiserID,
           string EventName,
           DateOnly EventDate,
           string? EventAddress,
           string? Description);
    }
}
