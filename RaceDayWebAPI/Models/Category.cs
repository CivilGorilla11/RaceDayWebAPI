namespace RaceDayWebAPI.Models
{
    public class Category
    {
        public int CategoryID { get; set; } 

        public int EventID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public decimal Distance { get; set; }

        public decimal EntryFee { get; set; }

        public int MaxParticipants { get; set; }

        // Remove automatic new Event() initialization so EF won't create/attach a phantom Event.
        // Make the navigation nullable so EF/consumers can set or load it explicitly.
        public Event? Event { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();   

        public ICollection<MapRoute> MapRoutes { get; set; } = new List<MapRoute>();
    }
}
