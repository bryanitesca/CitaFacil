using System;
using System.Linq;
using System.Threading.Tasks;
using CitaFacil.Constants;
using CitaFacil.Data;
using CitaFacil.Extensions;
using CitaFacil.Models;
using CitaFacil.Services.Usuarios;
using CitaFacil.Services.Notifications;
using CitaFacil.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CitaFacil.Controllers
{
    [Authorize]
    public class PacientesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUsuarioPerfilService _perfilService;
        private readonly ILogger<PacientesController> _logger;
        private readonly INotificationService _notificationService;

        public PacientesController(ApplicationDbContext context, IUsuarioPerfilService perfilService, ILogger<PacientesController> logger, INotificationService notificationService)
        {
            _context = context;
            _perfilService = perfilService;
            _logger = logger;
            _notificationService = notificationService;
        }

        // GET: Pacientes
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Pacientes.Include(p => p.Usuario);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Pacientes/Details/5
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paciente = await _context.Pacientes
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.id == id);
            if (paciente == null)
            {
                return NotFound();
            }

            return View(paciente);
        }

        // GET: Pacientes/Create
        [Authorize(Roles = Roles.Administrador)]
        public IActionResult Create()
        {
            TempData["ErrorMessage"] = "Utiliza la gestión de usuarios para crear pacientes.";
            return RedirectToAction("Create", "Usuarios");
        }

        // POST: Pacientes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Administrador)]
        public IActionResult Create([Bind("id,usuario_id,nombre,apellido,segundo_apellido,fecha_nacimiento,genero,direccion,nss")] Paciente paciente)
        {
            TempData["ErrorMessage"] = "Utiliza la gestión de usuarios para crear pacientes.";
            return RedirectToAction("Create", "Usuarios");
        }

        // GET: Pacientes/Edit/5
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null)
            {
                return NotFound();
            }
            TempData["ErrorMessage"] = "Utiliza la gestión de usuarios para editar pacientes.";
            return RedirectToAction("Edit", "Usuarios", new { id = paciente.usuario_id });
        }

        // POST: Pacientes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Administrador)]
        public IActionResult Edit(long id, [Bind("id,usuario_id,nombre,apellido,segundo_apellido,fecha_nacimiento,genero,direccion,nss")] Paciente paciente)
        {
            TempData["ErrorMessage"] = "Utiliza la gestión de usuarios para editar pacientes.";
            return RedirectToAction("Edit", "Usuarios", new { id = paciente.usuario_id });
        }

        // GET: Pacientes/Delete/5
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paciente = await _context.Pacientes
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.id == id);
            if (paciente == null)
            {
                return NotFound();
            }

            return View(paciente);
        }

        // POST: Pacientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente != null)
            {
                _context.Pacientes.Remove(paciente);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PacienteExists(long id)
        {
            return _context.Pacientes.Any(e => e.id == id);
        }

        // Vistas prototipo (sin lógica)
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Forbid();
            }

            var paciente = await _perfilService.ObtenerPacienteAsync(userId.Value);
            if (paciente == null)
            {
                var usuario = await _context.Usuarios.FindAsync(userId.Value);
                if (usuario == null)
                {
                    return NotFound();
                }

                paciente = new Paciente
                {
                    usuario_id = usuario.id,
                    nombre = usuario.nombre,
                    apellido = usuario.apellido ?? usuario.nombre,
                    segundo_apellido = usuario.segundo_apellido,
                    fecha_nacimiento = usuario.creado_el.Date > DateTime.Today.AddYears(-18)
                        ? DateTime.Today.AddYears(-18)
                        : usuario.creado_el.Date,
                    // No telefono field needed - removed per requirements
                };

                _context.Pacientes.Add(paciente);
                await _context.SaveChangesAsync();
            }

            var citas = await _context.Citas
                .Include(c => c.Doctor)
                    .ThenInclude(d => d.Usuario)
                .Where(c => c.paciente_id == paciente.id)
                .OrderBy(c => c.fecha)
                .ThenBy(c => c.hora)
                .ToListAsync();

            var hoy = DateTime.Today;
            var viewModel = new PatientDashboardViewModel
            {
                Paciente = paciente,
                ProximasCitas = citas.Where(c => c.fecha.Date >= hoy && 
                                               c.estado != "COMPLETADA" && 
                                               c.estado != "CANCELADA").ToList(),
                CitasPasadas = citas.Where(c => c.fecha.Date < hoy || 
                                              c.estado == "COMPLETADA").OrderByDescending(c => c.fecha).ThenByDescending(c => c.hora).ToList()
            };

            return View(viewModel);
        }

        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> Perfil()
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Forbid();
            }

            var paciente = await _perfilService.ObtenerPacienteAsync(userId.Value);
            if (paciente == null)
            {
                return NotFound();
            }

            return View(paciente);
        }

        // CALENDARIO PARA PACIENTES
        [HttpGet]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> Calendario()
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Forbid();
            }

            var paciente = await _perfilService.ObtenerPacienteAsync(userId.Value);
            if (paciente == null)
            {
                return NotFound();
            }

            ViewBag.CurrentPaciente = paciente;
            return View();
        }

        [HttpGet]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> GetCitasJson()
        {
            try
            {
                var userId = User.GetUserId();
                if (userId is null)
                {
                    return Json(new List<object>());
                }

                var paciente = await _perfilService.ObtenerPacienteAsync(userId.Value);
                if (paciente == null)
                {
                    return Json(new List<object>());
                }

                var citas = await _context.Citas
                    .Include(c => c.Doctor)
                    .ThenInclude(d => d.Usuario)
                    .Include(c => c.Doctor.Especialidad)
                    .Where(c => c.paciente_id == paciente.id &&
                               c.fecha >= DateTime.Today.AddMonths(-1) && 
                               c.fecha <= DateTime.Today.AddMonths(6))
                    .Select(c => new
                    {
                        id = c.id,
                        title = $"Dr. {c.Doctor.Usuario.nombre} - {c.Doctor.Especialidad.nombre}",
                        start = c.fecha.Add(c.hora).ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = c.fecha.Add(c.hora).AddMinutes(c.duracion_minutos).ToString("yyyy-MM-ddTHH:mm:ss"),
                        color = GetColorByEstadoPaciente(c.estado),
                        extendedProps = new
                        {
                            estado = c.estado,
                            motivo = c.motivo,
                            doctorNombre = $"Dr. {c.Doctor.Usuario.nombre}",
                            especialidad = c.Doctor.Especialidad != null ? c.Doctor.Especialidad.nombre : "General",
                        ubicacion = c.es_virtual
                            ? "Consulta virtual"
                            : (string.IsNullOrWhiteSpace(c.Doctor.consultorio) ? "Consultorio por confirmar" : c.Doctor.consultorio),
                            esVirtual = c.es_virtual,
                            notas = c.notas
                        }
                    })
                    .ToListAsync();

                return Json(citas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener citas para calendario del paciente");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> CancelarCita(long? id, [FromBody] CancelarCitaModel model, [FromQuery] long? citaId)
        {
            try
            {
                var targetCitaId = citaId ?? id;
                if (targetCitaId is null)
                {
                    return Json(new { success = false, message = "Identificador de cita inválido" });
                }

                var userId = User.GetUserId();
                if (userId is null)
                {
                    return Json(new { success = false, message = "Usuario no autenticado" });
                }

                var paciente = await _perfilService.ObtenerPacienteAsync(userId.Value);
                if (paciente == null)
                {
                    return Json(new { success = false, message = "Paciente no encontrado" });
                }

                var cita = await _context.Citas
                    .Include(c => c.Doctor)
                    .FirstOrDefaultAsync(c => c.id == targetCitaId.Value && c.paciente_id == paciente.id);

                if (cita == null)
                {
                    return Json(new { success = false, message = "Cita no encontrada" });
                }

                if (cita.estado == "CANCELADA")
                {
                    return Json(new { success = false, message = "La cita ya está cancelada" });
                }

                cita.estado = "CANCELADA";
                cita.actualizada_el = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Notificar al doctor
                await _notificationService.NotificarCitaCanceladaAsync(
                    cita.doctor_id,
                    cita.paciente_id,
                    cita.fecha.Add(cita.hora),
                    model.Motivo ?? "Cancelada por el paciente"
                );

                return Json(new { success = true, message = "Cita cancelada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar cita");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        [HttpGet]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> GetDoctoresDisponibles()
        {
            try
            {
                var doctores = await _context.Doctores
                    .Include(d => d.Usuario)
                    .Include(d => d.Especialidad)
                    .Where(d => d.Usuario.activo)
                    .Select(d => new
                    {
                        id = d.id,
                        nombre = $"Dr. {d.Usuario.nombre}",
                        especialidad = d.Especialidad.nombre,
                        foto = d.Usuario.foto_url
                    })
                    .ToListAsync();

                return Json(doctores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener doctores disponibles");
                return Json(new List<object>());
            }
        }

        [HttpGet]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> GetHorariosDisponibles(long doctorId, DateTime fecha)
        {
            try
            {
                // Horarios laborales típicos (8:00 AM - 5:00 PM)
                var horariosBase = new List<TimeSpan>();
                for (int hora = 8; hora < 17; hora++)
                {
                    horariosBase.Add(new TimeSpan(hora, 0, 0));
                    horariosBase.Add(new TimeSpan(hora, 30, 0));
                }

                // Obtener citas existentes para ese doctor en esa fecha
                var citasExistentes = await _context.Citas
                    .Where(c => c.doctor_id == doctorId && 
                               c.fecha.Date == fecha.Date && 
                               c.estado != "CANCELADA")
                    .Select(c => c.hora)
                    .ToListAsync();

                // Filtrar horarios disponibles
                var fechaConsulta = fecha.Date;
                var limiteReserva = DateTime.Today.AddDays(1);

                var horariosDisponibles = horariosBase
                    .Where(h => !citasExistentes.Contains(h))
                    .Where(h => fechaConsulta.Add(h) >= limiteReserva)
                    .Select(h => new
                    {
                        hora = h.ToString(@"hh\:mm"),
                        display = DateTime.Today.Add(h).ToString("hh:mm tt")
                    })
                    .ToList();

                return Json(horariosDisponibles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener horarios disponibles");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> ReagendarCita(long? id, [FromQuery] long? citaId)
        {
            try
            {
                var targetCitaId = citaId ?? id;
                if (targetCitaId is null)
                {
                    return Json(new { success = false, message = "Identificador de cita inválido" });
                }
                
                var userId = User.GetUserId();
                if (userId is null)
                {
                    return Json(new { success = false, message = "Usuario no autenticado" });
                }

                var paciente = await _perfilService.ObtenerPacienteAsync(userId.Value);
                if (paciente == null)
                {
                    return Json(new { success = false, message = "Paciente no encontrado" });
                }

                var cita = await _context.Citas
                    .Include(c => c.Doctor)
                    .FirstOrDefaultAsync(c => c.id == targetCitaId.Value && c.paciente_id == paciente.id);

                if (cita == null)
                {
                    return Json(new { success = false, message = "Cita no encontrada" });
                }

                if (cita.estado.Equals("CANCELADA", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "La cita ya fue cancelada" });
                }

                if (cita.Doctor == null)
                {
                    return Json(new { success = false, message = "La cita no tiene un doctor asignado" });
                }

                var redirectUrl = Url.Action("AgendarPaso2", "Citas", new
                {
                    especialidadId = cita.Doctor.especialidad_id,
                    doctorId = cita.doctor_id,
                    citaId = cita.id
                });

                return Json(new { success = true, redirectUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reagendar cita");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        // --- AJUSTES DEL PACIENTE ---
        
        // GET: /Pacientes/Ajustes
        [HttpGet]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> Ajustes()
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var paciente = await _context.Pacientes
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.usuario_id == userId.Value);
            
            if (paciente == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Paciente = paciente;
            return View();
        }

        // POST: /Pacientes/Ajustes
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> Ajustes(string nombre, string apellido, string segundoApellido, string celular, string direccion)
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var paciente = await _context.Pacientes
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.usuario_id == userId.Value);
            
            if (paciente == null)
            {
                return NotFound();
            }

            var nombreSanitizado = string.IsNullOrWhiteSpace(nombre) ? paciente.nombre : nombre.Trim();
            var apellidoSanitizado = string.IsNullOrWhiteSpace(apellido) ? paciente.apellido : apellido.Trim();
            var segundoSanitizado = string.IsNullOrWhiteSpace(segundoApellido) ? null : segundoApellido.Trim();
            var celularSanitizado = string.IsNullOrWhiteSpace(celular) ? null : celular.Trim();
            var direccionSanitizada = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();

            paciente.nombre = nombreSanitizado;
            paciente.apellido = apellidoSanitizado ?? paciente.apellido;
            paciente.segundo_apellido = segundoSanitizado;
            paciente.direccion = direccionSanitizada;
            
            if (paciente.Usuario != null)
            {
                paciente.Usuario.nombre = nombreSanitizado;
                paciente.Usuario.apellido = apellidoSanitizado ?? paciente.Usuario.apellido;
                paciente.Usuario.segundo_apellido = segundoSanitizado;
                paciente.Usuario.celular = celularSanitizado;
            }
            
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Configuración actualizada correctamente";
            return RedirectToAction(nameof(Ajustes));
        }

        // --- NOTIFICACIONES ---
        
        // GET: /Pacientes/Notificaciones
        [HttpGet]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> Notificaciones()
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var paciente = await _context.Pacientes
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.usuario_id == userId.Value);
            
            if (paciente == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Paciente = paciente;
            return View();
        }

        // GET: /Pacientes/ObtenerNotificaciones
        [HttpGet]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> ObtenerNotificaciones()
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Json(new { success = false, message = "Usuario no autenticado" });
            }

            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.usuario_id == userId.Value);
            if (paciente == null)
            {
                return Json(new { success = false, message = "Paciente no encontrado" });
            }

            var notificaciones = await _notificationService.ObtenerNotificacionesPacienteAsync(paciente.id);
            var noLeidas = await _notificationService.ContarNotificacionesNoLeidasPacienteAsync(paciente.id);

            return Json(new 
            { 
                success = true, 
                notificaciones = notificaciones,
                totalNoLeidas = noLeidas
            });
        }

        // POST: /Pacientes/MarcarNotificacionLeida
        [HttpPost]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> MarcarNotificacionLeida([FromBody] long notificacionId)
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Json(new { success = false, message = "Usuario no autenticado" });
            }

            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.usuario_id == userId.Value);
            if (paciente == null)
            {
                return Json(new { success = false, message = "Paciente no encontrado" });
            }

            var resultado = await _notificationService.MarcarNotificacionLeidaPacienteAsync(notificacionId, paciente.id);

            return Json(new { success = resultado.Exito, message = resultado.Mensaje });
        }

        // POST: /Pacientes/MarcarTodasLeidas
        [HttpPost]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Json(new { success = false, message = "Usuario no autenticado" });
            }

            var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.usuario_id == userId.Value);
            if (paciente == null)
            {
                return Json(new { success = false, message = "Paciente no encontrado" });
            }

            var resultado = await _notificationService.MarcarTodasLeidasPacienteAsync(paciente.id);

            return Json(new { success = resultado.Exito, message = resultado.Mensaje });
        }

        // MÉTODOS AUXILIARES
        private string GetColorByEstadoPaciente(string estado)
        {
            return estado?.ToUpperInvariant() switch
            {
                "PENDIENTE" => "#f59e0b",      // Amber
                "CONFIRMADA" => "#10b981",     // Emerald
                "COMPLETADA" => "#6b7280",     // Gray
                "CANCELADA" => "#ef4444",      // Red
                _ => "#6b7280"                 // Gray default
            };
        }
    }
}

// MODELOS PARA PACIENTES
public class CancelarCitaModel
{
    public string? Motivo { get; set; }
}
