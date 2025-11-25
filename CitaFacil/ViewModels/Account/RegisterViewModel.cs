using System;
using System.ComponentModel.DataAnnotations;

namespace CitaFacil.ViewModels.Account
{
    public class RegisterViewModel
    {
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

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [StringLength(255)]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string contraseña { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes confirmar la contraseña.")]
        [DataType(DataType.Password)]
        [Compare("contraseña", ErrorMessage = "Las contraseñas no coinciden.")]
        public string confirmar_contraseña { get; set; } = string.Empty;

        [StringLength(20)]
        public string? celular { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime fecha_nacimiento { get; set; }

        [StringLength(50)]
        public string? genero { get; set; }

        [StringLength(255)]
        public string? direccion { get; set; }
    }
}

