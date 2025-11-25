using System.Threading.Tasks;
using CitaFacil.Data;
using CitaFacil.Models;
using Microsoft.EntityFrameworkCore;

namespace CitaFacil.Services.Usuarios
{
    public class UsuarioPerfilService : IUsuarioPerfilService
    {
        private readonly ApplicationDbContext _context;

        public UsuarioPerfilService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Doctor?> ObtenerDoctorAsync(long usuarioId)
        {
            return _context.Doctores
                .Include(d => d.Usuario)
                .Include(d => d.Especialidad)
                .FirstOrDefaultAsync(d => d.usuario_id == usuarioId);
        }

        public Task<Paciente?> ObtenerPacienteAsync(long usuarioId)
        {
            return _context.Pacientes
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.usuario_id == usuarioId);
        }
    }
}

