using System.Threading.Tasks;
using CitaFacil.Models;

namespace CitaFacil.Services.Usuarios
{
    public interface IUsuarioPerfilService
    {
        Task<Doctor?> ObtenerDoctorAsync(long usuarioId);
        Task<Paciente?> ObtenerPacienteAsync(long usuarioId);
    }
}

