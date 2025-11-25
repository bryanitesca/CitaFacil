using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitaFacil.Models
{
    [Index(nameof(licencia), IsUnique = true)] // Para la restricción UNIQUE
    public class Doctor
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

        [Required(ErrorMessage = "La licencia es obligatoria")]
        [StringLength(100)]
        public string licencia { get; set; } = null!;

        [StringLength(255)]
        public string? consultorio { get; set; }

        [StringLength(500)]
        public string? biografia { get; set; }

        [Required]
        [ForeignKey("Especialidad")]
        public long especialidad_id { get; set; }

        // Propiedades de navegación
        public virtual Usuario Usuario { get; set; } = null!; // El usuario al que está ligado este perfil
        public virtual Especialidad Especialidad { get; set; } = null!; // La especialidad del doctor
        public virtual ICollection<Cita>? Citas { get; set; } // Las citas de este doctor

        // Propiedades computadas
        public string NombreCompleto => $"{Usuario?.nombre} {Usuario?.apellido}".Trim();
        
        public string FotoUrlOPlaceholder
        {
            get
            {
                if (!string.IsNullOrEmpty(Usuario?.foto_url))
                    return Usuario.foto_url;
                
                // Generar placeholder con iniciales
                var initials = "";
                if (!string.IsNullOrEmpty(Usuario?.nombre))
                    initials += Usuario.nombre[0];
                if (!string.IsNullOrEmpty(Usuario?.apellido))
                    initials += Usuario.apellido[0];
                
                return $"https://ui-avatars.com/api/?name={initials}&background=0d6efd&color=fff&size=200";
            }
        }
    }
}
