namespace RaceDayWebAPI.DTOs
{
    public class CategoryDtos
    {
        public record CreateCategoryRequest(string CategoryName, decimal Distance, decimal EntryFee, int MaxParticipants);

        public record UpdateCategoryRequest(string? CategoryName, decimal? Distance, decimal? EntryFee, int? MaxParticipants);

        public record CategoryResponse(
            int CategoryID,
            int EventID,
            string CategoryName,
            decimal Distance,
            decimal EntryFee,
            int MaxParticipants);

        public record CreateRouteRequest(string RouteName, decimal? ElevationGain, string? MapLink);

        public record RouteResponse(int RouteID, int CategoryID, string RouteName, decimal? ElevationGain, string? MapLink);
    }
}
