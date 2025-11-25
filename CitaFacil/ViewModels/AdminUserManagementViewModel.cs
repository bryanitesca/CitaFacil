using System;
using System.Collections.Generic;

namespace CitaFacil.ViewModels
{
    public class AdminUserManagementViewModel
    {
        public int TotalUsuarios { get; set; }
        public int TotalDoctores { get; set; }
        public int TotalPacientes { get; set; }
        public IReadOnlyList<AdminUserRow> Usuarios { get; set; } = Array.Empty<AdminUserRow>();
    }

    public record AdminUserRow(long UsuarioId, string NombreCompleto, string Correo, string Rol, DateTime FechaRegistro, bool Activo);
}
