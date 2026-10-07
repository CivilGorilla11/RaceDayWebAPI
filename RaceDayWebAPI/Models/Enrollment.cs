namespace RaceDayWebAPI.Models
{
    public class Enrollment
    {
        public int EnrollmentID { get; set; } 

        public int ParticipantID { get; set; }

        public int CategoryID { get; set; }

        public DateOnly EnrollmentDate { get; set; }

        public string Status { get; set; } = string.Empty;


        public Participants ? Participants {  get; set; }
        public Category? Category { get; set; } 

        public ICollection<Result>Results { get; set; } = new List<Result>();
    }
}
