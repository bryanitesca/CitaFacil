using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CitaFacil.ViewModels
{
    // ViewModel para el formulario de EDICIÓN
    public class EditUserViewModel
    {
        // --- Campos de Usuario ---
        [Required]
        public long id { get; set; } // El ID es necesario para saber a quién editamos

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(120)]
        public string nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(120)]
        public string apellido { get; set; } = string.Empty;

        [StringLength(120)]
        public string? segundo_apellido { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(60)]
        public string usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [StringLength(255)]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio.")]
        public string rol { get; set; } = string.Empty;

        [StringLength(20)]
        public string? celular { get; set; }

        // --- NOTA: ---
        // La contraseña se maneja por separado (ej. en una página "Cambiar Contraseña")
        // No la cargamos aquí por seguridad.

        // --- Campos de Doctor (Opcionales) ---
        [StringLength(100)]
        public string? licencia { get; set; }

        public long? especialidad_id { get; set; }

        [StringLength(255)]
        public string? consultorio { get; set; }

        [StringLength(500)]
        public string? biografia { get; set; }

        // --- Campos de Paciente (Opcionales) ---
        [DataType(DataType.Date)]
        public DateTime? fecha_nacimiento { get; set; }

        [StringLength(50)]
        public string? genero { get; set; }

        [StringLength(255)]
        public string? direccion { get; set; }


        [Display(Name = "Fotografía (jpg o png)")]
        public IFormFile? foto { get; set; }

        public string? foto_actual { get; set; }
    }
}
