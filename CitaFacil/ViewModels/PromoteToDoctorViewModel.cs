using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CitaFacil.ViewModels
{
    public class PromoteToDoctorViewModel
    {
        [Required]
        public long usuario_id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(120)]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100)]
        public string apellido { get; set; } = string.Empty;

        [StringLength(100)]
        public string? segundo_apellido { get; set; }

        [Required(ErrorMessage = "La licencia profesional es obligatoria")]
        [StringLength(100)]
        public string licencia { get; set; } = string.Empty;

        [StringLength(255)]
        public string? consultorio { get; set; }

        [StringLength(500)]
        public string? biografia { get; set; }

        [Required(ErrorMessage = "La especialidad es obligatoria")]
        public long especialidad_id { get; set; }

        [Required(ErrorMessage = "La foto del doctor es obligatoria")]
        public IFormFile? foto { get; set; }

        // For display purposes
        public string? usuario_nombre { get; set; }
        public string? usuario_correo { get; set; }
    }
}