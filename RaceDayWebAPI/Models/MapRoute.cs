using System.ComponentModel.DataAnnotations;

namespace RaceDayWebAPI.Models
{
    public class MapRoute
    {
        [Key]
        public int RouteID { get; set; }

        public int CategoryID { get; set; }

        public string RouteName { get; set; } = string.Empty;

        public decimal? ElevationGain { get; set; }

        public string? MapLink { get; set; }

        public Category? Category { get; set; }
    }
}
