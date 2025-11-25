using System;
using System.Collections.Generic;

namespace CitaFacil.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsuarios { get; set; }
        public int TotalDoctores { get; set; }
        public int TotalPacientes { get; set; }
        public int CitasHoy { get; set; }
        public int NuevosUsuariosSemana { get; set; }
        public int CitasSemana { get; set; }
        public int CitasSemanaAnterior { get; set; }
        public IReadOnlyList<BarChartItem> VolumenCitasSemanal { get; set; } = Array.Empty<BarChartItem>();
        public IReadOnlyList<AdminUserSummary> UsuariosRecientes { get; set; } = Array.Empty<AdminUserSummary>();
        public IReadOnlyList<AdminActivityItem> ActividadesRecientes { get; set; } = Array.Empty<AdminActivityItem>();
    }

    public record BarChartItem(string Etiqueta, int Valor);

    public record AdminUserSummary(long UsuarioId, string NombreCompleto, string Correo, string Rol, DateTime FechaRegistro, bool Activo);

    public record AdminActivityItem(string Icono, string Titulo, string Detalle, DateTime FechaEvento);
}
