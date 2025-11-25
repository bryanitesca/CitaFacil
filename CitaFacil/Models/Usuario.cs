using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CitaFacil.Models
{
    [Index(nameof(usuario), IsUnique = true)]
    [Index(nameof(correo), IsUnique = true)]
    public class Usuario
    {
        [Key]
        public long id { get; set; }

        [StringLength(120)]
        public string? apellido { get; set; }

        [StringLength(120)]
        public string? segundo_apellido { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string nombre { get; set; } = null!;

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [StringLength(60)]
        public string usuario { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(255)]
        [DataType(DataType.Password)]
        public string contraseña { get; set; } = null!; // Recuerda guardar esto como HASH

        [Required(ErrorMessage = "El rol es obligatorio")]
        [StringLength(50)]
        public string rol { get; set; } = null!;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [StringLength(255)]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        public string correo { get; set; } = null!;

        [StringLength(20)]
        public string? celular { get; set; }

        // Note: foto_url will only be used for doctors, managed by admin
        [StringLength(255)]
        public string? foto_url { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime creado_el { get; set; } = DateTime.UtcNow;

        public bool activo { get; set; } = true;

        // Propiedades de navegación (Relación 1 a 1)
        public virtual Paciente? Paciente { get; set; }
        public virtual Doctor? Doctor { get; set; }
    }
}
