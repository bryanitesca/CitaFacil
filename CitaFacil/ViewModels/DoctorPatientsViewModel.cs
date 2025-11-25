using System;
using System.Collections.Generic;

namespace CitaFacil.ViewModels
{
    public class DoctorPatientsViewModel
    {
        public int TotalPacientes { get; set; }

        public int PacientesConfirmados { get; set; }

        public int PacientesPendientes { get; set; }

        public int PacientesSinSeguimiento { get; set; }

        public IList<DoctorPatientRow> Pacientes { get; set; } = new List<DoctorPatientRow>();

        public int TotalFiltrados { get; set; }

        public int PaginaActual { get; set; }

        public int TotalPaginas { get; set; }

        public string Busqueda { get; set; } = string.Empty;

        public string EstadoFiltro { get; set; } = "TODOS";

        public class DoctorPatientRow
        {
            public long PacienteId { get; set; }

            public string NombreCompleto { get; set; } = string.Empty;

            public string AvatarIniciales { get; set; } = string.Empty;

            public string? Correo { get; set; }

            public string? Telefono { get; set; }

            public DateTime? ProximaCita { get; set; }

            public string ProximaCitaEstado { get; set; } = string.Empty;

            public string ProximaCitaEstadoCodigo { get; set; } = string.Empty;
        }
    }
}

