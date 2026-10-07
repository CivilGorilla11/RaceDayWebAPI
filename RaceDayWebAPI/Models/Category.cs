namespace RaceDayWebAPI.Models
{
    public class Category
    {
        public int CategoryID { get; set; } 

        public int EventID { get; set; }

        public string CategoryName { get; set; }

        public decimal Distance { get; set; }

        public decimal EntryFee { get; set; }

        public int MaxParticipants { get; set; }

        public Event? Event { get; set; }

        public ICollection<Enrollment>Enrollments { get; set; } = new List<Enrollment>();   

        public ICollection<MapRoute> MapRoutes { get; set; } = new List<MapRoute>();

    }
}
