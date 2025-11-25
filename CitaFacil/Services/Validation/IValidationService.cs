using System.Threading.Tasks;

namespace CitaFacil.Services.Validation
{
    public interface IValidationService
    {
        Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null);
        Task<bool> IsPhoneUniqueAsync(string phone, int? excludeUserId = null);
        bool IsValidEmail(string email);
        bool IsValidPhone(string phone);
        bool IsValidDate(DateTime date);
        bool IsValidTimeSlot(TimeSpan startTime, TimeSpan endTime);
        Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeCitaId = null);
        Task<bool> IsPatientAvailableAsync(int patientId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeCitaId = null);
        ValidationResult ValidateAppointmentData(DateTime date, TimeSpan startTime, TimeSpan endTime, int doctorId, int patientId);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }
    }
}