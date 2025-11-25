using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CitaFacil.Data;
using Microsoft.EntityFrameworkCore;

namespace CitaFacil.Services.Validation
{
    public class ValidationService : IValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(ApplicationDbContext context, ILogger<ValidationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null)
        {
            try
            {
                var query = _context.Usuarios.Where(u => u.correo.ToLower() == email.ToLower());
                
                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.id != excludeUserId.Value);
                }

                var exists = await query.AnyAsync();
                return !exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando email único: {Email}", email);
                return false;
            }
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeUserId = null)
        {
            try
            {
                var cleanPhone = CleanPhoneNumber(phone);
                var query = _context.Usuarios.Where(u => u.celular == cleanPhone);
                
                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.id != excludeUserId.Value);
                }

                var exists = await query.AnyAsync();
                return !exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando teléfono único: {Phone}", phone);
                return false;
            }
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.IgnoreCase);
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            var cleanPhone = CleanPhoneNumber(phone);
            return cleanPhone.Length == 10 && cleanPhone.All(char.IsDigit);
        }

        public bool IsValidDate(DateTime date)
        {
            var today = DateTime.Today;
            var maxDate = today.AddYears(1);
            
            return date.Date >= today && date.Date <= maxDate;
        }

        public bool IsValidTimeSlot(TimeSpan startTime, TimeSpan endTime)
        {
            // Horarios de trabajo: 8:00 AM a 6:00 PM
            var workStart = new TimeSpan(8, 0, 0);
            var workEnd = new TimeSpan(18, 0, 0);
            
            // La cita debe estar dentro del horario de trabajo
            if (startTime < workStart || endTime > workEnd)
                return false;
            
            // La hora de fin debe ser después de la hora de inicio
            if (endTime <= startTime)
                return false;
            
            // Duración mínima de 30 minutos, máxima de 2 horas
            var duration = endTime - startTime;
            if (duration < TimeSpan.FromMinutes(30) || duration > TimeSpan.FromHours(2))
                return false;
            
            return true;
        }

        public async Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeCitaId = null)
        {
            try
            {
                var query = _context.Citas
                    .Where(c => c.doctor_id == doctorId && 
                               c.fecha.Date == date.Date &&
                               c.estado != "Cancelada");

                if (excludeCitaId.HasValue)
                {
                    query = query.Where(c => c.id != excludeCitaId.Value);
                }

                var existingAppointments = await query.ToListAsync();

                foreach (var appointment in existingAppointments)
                {
                    var appointmentStart = appointment.hora;
                    var appointmentEnd = appointment.hora.Add(TimeSpan.FromMinutes(appointment.duracion_minutos));

                    // Verificar si hay superposición
                    if (startTime < appointmentEnd && endTime > appointmentStart)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando disponibilidad del doctor {DoctorId}", doctorId);
                return false;
            }
        }

        public async Task<bool> IsPatientAvailableAsync(int patientId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeCitaId = null)
        {
            try
            {
                var query = _context.Citas
                    .Where(c => c.paciente_id == patientId && 
                               c.fecha.Date == date.Date &&
                               c.estado != "Cancelada");

                if (excludeCitaId.HasValue)
                {
                    query = query.Where(c => c.id != excludeCitaId.Value);
                }

                var existingAppointments = await query.ToListAsync();

                foreach (var appointment in existingAppointments)
                {
                    var appointmentStart = appointment.hora;
                    var appointmentEnd = appointment.hora.Add(TimeSpan.FromMinutes(appointment.duracion_minutos));

                    // Verificar si hay superposición
                    if (startTime < appointmentEnd && endTime > appointmentStart)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando disponibilidad del paciente {PatientId}", patientId);
                return false;
            }
        }

        public ValidationResult ValidateAppointmentData(DateTime date, TimeSpan startTime, TimeSpan endTime, int doctorId, int patientId)
        {
            var result = new ValidationResult { IsValid = true };

            // Validar fecha
            if (!IsValidDate(date))
            {
                result.AddError("La fecha debe estar entre hoy y un año en el futuro");
            }

            // Validar horario
            if (!IsValidTimeSlot(startTime, endTime))
            {
                result.AddError("El horario debe estar entre 8:00 AM y 6:00 PM, con duración entre 30 minutos y 2 horas");
            }

            // Validar que no sea fin de semana
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                result.AddError("No se pueden programar citas los fines de semana");
            }

            // Validar que no sea en el pasado (con margen de tiempo)
            var minimumTime = DateTime.Now.AddHours(2);
            var appointmentDateTime = date.Add(startTime);
            
            if (appointmentDateTime < minimumTime)
            {
                result.AddError("Las citas deben programarse con al menos 2 horas de anticipación");
            }

            return result;
        }

        private string CleanPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            return new string(phone.Where(char.IsDigit).ToArray());
        }
    }
}