using CitaFacil.Models;

namespace CitaFacil.ViewModels
{
    public class CalendarioViewModel
    {
        public IList<Cita> Citas { get; set; } = new List<Cita>();
        public IList<Doctor> Doctores { get; set; } = new List<Doctor>();
        public IList<Especialidad> Especialidades { get; set; } = new List<Especialidad>();
        public IList<Paciente> Pacientes { get; set; } = new List<Paciente>();
        
        // Filtros
        public string? FiltroEstado { get; set; }
        public long? FiltroEspecialidad { get; set; }
        public long? FiltroDoctor { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? VistaActual { get; set; } = "dayGridMonth";
        
        // Estados disponibles
        public static readonly Dictionary<string, string> EstadosDisponibles = new()
        {
            { "", "Todos" },
            { "PENDIENTE", "Pendientes" },
            { "CONFIRMADA", "Confirmadas" },
            { "COMPLETADA", "Completadas" },
            { "CANCELADA", "Canceladas" }
        };
        
        // Vistas disponibles del calendario
        public static readonly Dictionary<string, string> VistasDisponibles = new()
        {
            { "dayGridMonth", "Mes" },
            { "timeGridWeek", "Semana" },
            { "timeGridDay", "Día" },
            { "listWeek", "Lista" }
        };
    }
}