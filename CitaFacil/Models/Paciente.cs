using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitaFacil.Models
{
    public class Paciente
    {
        [Key]
        public long id { get; set; }

        [Required]
        [ForeignKey("Usuario")]
        public long usuario_id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(120)]
        public string nombre { get; set; } = null!;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100)]
        public string apellido { get; set; } = null!;

        [StringLength(100)]
        public string? segundo_apellido { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime fecha_nacimiento { get; set; }

        [StringLength(50)]
        public string? genero { get; set; }

        [StringLength(255)]
        public string? direccion { get; set; }

        // Propiedades de navegación
        public virtual Usuario Usuario { get; set; } = null!; // El usuario al que está ligado este perfil
        public virtual ICollection<Cita>? Citas { get; set; } // Las citas de este paciente
    }
}
