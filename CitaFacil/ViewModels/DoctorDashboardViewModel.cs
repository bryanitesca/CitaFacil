using CitaFacil.Models;
using System.Collections.Generic;

namespace CitaFacil.ViewModels
{
    // Usaremos esta clase para pasar los datos al Dashboard del Doctor
    public class DoctorDashboardViewModel
    {
        public List<Cita> CitasDeHoy { get; set; } = new List<Cita>();

        // Esto es para mostrar los detalles del paciente seleccionado
        // (En una versión futura, lo cargarías con JS al hacer clic en una cita)
        public Paciente? PacienteSeleccionado { get; set; }

        public Cita? CitaSeleccionada { get; set; }

        public List<Cita> HistorialPaciente { get; set; } = new List<Cita>();

        public List<Cita> NotasPaciente { get; set; } = new List<Cita>();
    }
}
