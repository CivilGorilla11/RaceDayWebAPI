namespace RaceDayWebAPI.Models
{
    public class Result
    {
        public int ResultID { get; set; }

        public int EnrollmentID { get; set; }

        public TimeOnly FinishTime { get; set; }

        public int Position { get; set; }

        public string Race { get; set; }

        public Enrollment ? Enrollment { get; set; }
    }
}
