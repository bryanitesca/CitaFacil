using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CitaFacil.ViewModels
{
    public class ScheduleStep1ViewModel
    {
        public string? Busqueda { get; init; }

        public IReadOnlyList<ScheduleSpecialtyItem> Especialidades { get; init; } = Array.Empty<ScheduleSpecialtyItem>();

        public int TotalEspecialidades => Especialidades.Count;

        public long? CitaId { get; init; }
    }

    public record ScheduleSpecialtyItem(
        long Id,
        string Nombre,
        string? Descripcion,
        bool Activa,
        int TotalDoctores,
        string? Icono);

    public class ScheduleStep2ViewModel
    {
        public long EspecialidadId { get; init; }

        public string EspecialidadNombre { get; init; } = string.Empty;

        public string? EspecialidadDescripcion { get; init; }

        public IReadOnlyList<ScheduleDoctorItem> Doctores { get; init; } = Array.Empty<ScheduleDoctorItem>();

        public long? DoctorSeleccionadoId { get; init; }

        public ScheduleDoctorDetail? DoctorSeleccionado { get; init; }

        public IReadOnlyList<ScheduleDayAvailability> Agenda { get; init; } = Array.Empty<ScheduleDayAvailability>();

        public DateOnly? FechaSeleccionada { get; init; }

        public TimeSpan? HoraSeleccionada { get; init; }

        public bool TieneDisponibilidad { get; init; }

        public long? CitaId { get; init; }
    }

    public record ScheduleDoctorItem(
        long Id,
        string NombreCompleto,
        string Especialidad,
        string? FotoUrl,
        string? Resumen,
        bool Seleccionado);

    public record ScheduleDoctorDetail(
        long Id,
        string NombreCompleto,
        string? FotoUrl,
        string Especialidad,
        string? Biografia,
        string? Consultorio,
        string? TelefonoContacto);

    public record ScheduleDayAvailability(
        DateOnly Fecha,
        string EtiquetaCorta,
        bool EsHoy,
        bool EstaDisponible,
        IReadOnlyList<ScheduleHourAvailability> Horas);

    public record ScheduleHourAvailability(
        TimeSpan Hora,
        bool Disponible,
        bool Seleccionado);

    public class ScheduleConfirmationViewModel
    {
        public long EspecialidadId { get; init; }

        public long DoctorId { get; init; }

        public string EspecialidadNombre { get; init; } = string.Empty;

        public string DoctorNombre { get; init; } = string.Empty;

        public string? DoctorFoto { get; init; }

        public string PacienteNombre { get; init; } = string.Empty;

        public string? PacienteCorreo { get; init; }

        public string? PacienteTelefono { get; init; }

        public DateOnly Fecha { get; init; }

        public TimeSpan Hora { get; init; }

        public bool EsVirtual { get; init; }

        public string? Ubicacion { get; init; }

        public string? MensajeEstado { get; init; }

        public bool PuedeConfirmar { get; init; }

        public string DoctorEspecialidad { get; init; } = string.Empty;

        public long? CitaId { get; init; }
    }

    public class ScheduleConfirmationInput
    {
        public long EspecialidadId { get; set; }

        public long DoctorId { get; set; }

        public DateOnly Fecha { get; set; }

        public TimeSpan Hora { get; set; }

        public bool EsVirtual { get; set; }

        [StringLength(500)]
        public string? Motivo { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public long? CitaId { get; set; }
    }
}

