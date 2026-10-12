namespace RaceDayWebAPI.Models
{
    public class Event
    {
        public int EventID { get; set; } 

        public int OrganiserID { get; set; }

        
        public string EventName { get; set; }

        public DateOnly EventDate   { get; set; }

        public string EventAddress { get; set; }

        public string Description { get; set; }

        public Organiser ? Organiser {  get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
