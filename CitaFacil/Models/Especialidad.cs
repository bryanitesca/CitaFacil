using System.ComponentModel.DataAnnotations;

namespace CitaFacil.Models
{
    public class Especialidad
    {
        [Key]
        public long id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string nombre { get; set; } = null!;

        [StringLength(255)]
        public string? descripcion { get; set; }

        [StringLength(120)]
        public string? icono { get; set; }

        public bool activa { get; set; } = true;

        // Propiedad de navegación: Una especialidad puede tener muchos doctores
        public virtual ICollection<Doctor>? Doctores { get; set; }
    }
}
