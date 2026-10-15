using System.ComponentModel.DataAnnotations;

namespace RaceDayWebAPI.Models
{
    public class Participant
    {
        public int ParticipantID { get; set; } = 0;

        //[Required]
        public string Name { get; set; } = string.Empty;

       /// <summary>
       /// [Required]
       /// </summary>
        public string Email { get; set; } = string.Empty;

       /// <summary>
        public string? Password { get; set; } = null;

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();  
    }
}
