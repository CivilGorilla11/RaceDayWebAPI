namespace RaceDayWebAPI.Models
{
    public class Organiser
    {
        public int OrganiserID { get; set; }

        public string Name { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public ICollection<Event> Events { get; set; } = new List<Event>(); 
    }
}
