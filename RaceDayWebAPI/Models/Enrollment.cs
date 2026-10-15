
using System;
using System.Collections.Generic;


namespace RaceDayWebAPI.Models
{
    public class Enrollment
    {
        public int EnrollmentID { get; set; }

        public int ParticipantID { get; set; }

        public int CategoryID { get; set; }

        public DateOnly EnrollmentDate { get; set; }

        public string Status { get; set; } = string.Empty;

        // Navigation property: matches the existing model type name 'Participants'
        public Participant? Participant { get; set; }
        public Category? Category { get; set; }

        public ICollection<Result> Results { get; set; } = new List<Result>();
    }
}

