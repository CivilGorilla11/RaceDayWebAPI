namespace RaceDayWebAPI.Models
{
    public class Participants
    {
        public int ParticipantID { get; set; } 

        public string Name { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
