namespace RaceDayWebAPI.Models
{
    public class Event
    {
        public int EventID { get; set; } = 0;

        public int OrganiserID { get; set; } = 0;

        
        public string EventName { get; set; } = string.Empty;

        public DateOnly EventDate   { get; set; }

        public string? EventAddress { get; set; } = string.Empty;    

        public string? Description { get; set; } 

        public Organiser? Organiser { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
