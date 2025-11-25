using System;
using System.Collections.Generic;

namespace CitaFacil.ViewModels
{
    public class AdminSpecialtiesViewModel
    {
        public int TotalEspecialidades { get; set; }
        public int EspecialidadesActivas { get; set; }
        public IReadOnlyList<AdminSpecialtyItem> Especialidades { get; set; } = Array.Empty<AdminSpecialtyItem>();
    }

    public record AdminSpecialtyItem(long Id, string Nombre, string? Descripcion, bool Activa, int DoctoresAsociados);
}
