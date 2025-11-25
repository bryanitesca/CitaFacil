using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CitaFacil.Models;
using CitaFacil.Services.Notifications.Models;

namespace CitaFacil.ViewModels
{
    public class DoctorPatientDetailViewModel
    {
        public long PacienteId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string AvatarIniciales { get; set; } = string.Empty;

        public string? Correo { get; set; }

        public string? Telefono { get; set; }

        public DateTime? FechaNacimiento { get; set; }


        public string? Genero { get; set; }

        public string? Direccion { get; set; }

        public Paciente Paciente { get; set; } = null!;

        public Usuario Usuario { get; set; } = null!;

        public IList<Cita> Historial { get; set; } = new List<Cita>();

        public int TotalCitas { get; set; }

        public int CitasPendientes { get; set; }

        public int CitasConfirmadas { get; set; }

        public int CitasCompletadas { get; set; }

        public int CitasCanceladas { get; set; }
    }

    public class DoctorPatientContactViewModel
    {
        public long PacienteId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string AvatarIniciales { get; set; } = string.Empty;

        public string? Correo { get; set; }

        public string? Telefono { get; set; }

        public DoctorPatientContactForm Formulario { get; set; } = new();

        public IReadOnlyList<NotificationRecord> Historial { get; set; } = Array.Empty<NotificationRecord>();
    }

    public class DoctorPatientContactForm
    {
        [Required(ErrorMessage = "El asunto es obligatorio.")]
        [StringLength(120)]
        public string Asunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es obligatorio.")]
        [StringLength(1000)]
        public string Mensaje { get; set; } = string.Empty;

        // Solo notificaciones del sistema - sin campos de email/SMS
    }
}

