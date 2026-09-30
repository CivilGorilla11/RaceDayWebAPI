namespace RaceDayWebAPI.DTOs
{
    public class Enrollment
    {
        public record CreateEnrollmentRequest(int CategoryID);

        public record EnrollmentResponse(
            int EnrollmentID,
            int ParticipantID,
            int CategoryID,
            DateOnly EnrollmentDate,
            string Status);

        public record CreateResultRequest(int EnrollmentID, TimeOnly FinishTime, int? Position, string Race);

        public record UpdateResultRequest(TimeOnly? FinishTime, int? Position, string? Race);

        public record UpdateResultResponse(int ResultId, int EnrollmentID, TimeOnly FinishTime, int? Position, string Race);
    }
}
