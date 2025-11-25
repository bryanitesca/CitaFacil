using System.Collections.Generic;
using CitaFacil.Models;

namespace CitaFacil.ViewModels
{
    public class PatientDashboardViewModel
    {
        public Paciente Paciente { get; set; } = null!;
        public IList<Cita> ProximasCitas { get; set; } = new List<Cita>();
        public IList<Cita> CitasPasadas { get; set; } = new List<Cita>();
    }
}

