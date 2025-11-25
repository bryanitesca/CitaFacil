using System.ComponentModel.DataAnnotations;

namespace CitaFacil.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Debes ingresar tu correo o usuario.")]
        [StringLength(120)]
        public string identificador { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(255)]
        [DataType(DataType.Password)]
        public string contraseña { get; set; } = string.Empty;

        public bool recordar { get; set; }
    }
}

