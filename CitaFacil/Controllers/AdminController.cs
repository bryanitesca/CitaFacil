using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CitaFacil.Constants;
using CitaFacil.Data;
using CitaFacil.Models;
using CitaFacil.ViewModels;
using CitaFacil.ViewModels.Admin;
using CitaFacil.Services.Storage;
using CitaFacil.Services.Notifications;
using CitaFacil.Services.DatabaseBackup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CitaFacil.Controllers
{
    // ¡MUY IMPORTANTE! Asegura que solo los admins puedan entrar
    [Authorize(Roles = Roles.Administrador)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageStorageService _imageStorageService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdminController> _logger;
        private readonly IDatabaseBackupService _backupService;

        public AdminController(ApplicationDbContext context, IImageStorageService imageStorageService, INotificationService notificationService, IDatabaseBackupService backupService, ILogger<AdminController> logger)
        {
            _context = context;
            _imageStorageService = imageStorageService;
            _notificationService = notificationService;
            _backupService = backupService;
            _logger = logger;
        }

        // Esta es la página principal del Dashboard
        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.Today;
            var inicioSemana = hoy.AddDays(-6);
            var inicioSemanaAnterior = hoy.AddDays(-13);
            var cultura = new CultureInfo("es-MX");

            var totalUsuarios = await _context.Usuarios.CountAsync();
            var totalDoctores = await _context.Usuarios.CountAsync(u => u.rol == Roles.Doctor);
            var totalPacientes = await _context.Usuarios.CountAsync(u => u.rol == Roles.Paciente);
            var citasHoy = await _context.Citas.CountAsync(c => c.fecha.Date == hoy);

            var nuevosUsuariosSemana = await _context.Usuarios.CountAsync(u => u.creado_el >= inicioSemana);
            var citasSemana = await _context.Citas.CountAsync(c => c.fecha.Date >= inicioSemana && c.fecha.Date <= hoy);
            var citasSemanaAnterior = await _context.Citas.CountAsync(c => c.fecha.Date >= inicioSemanaAnterior && c.fecha.Date < inicioSemana);

            var citasPorDia = await _context.Citas
                .Where(c => c.fecha.Date >= inicioSemana && c.fecha.Date <= hoy)
                .GroupBy(c => c.fecha.Date)
                .Select(g => new { Fecha = g.Key, Total = g.Count() })
                .ToListAsync();

            var barras = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var fecha = inicioSemana.AddDays(i);
                    var total = citasPorDia.FirstOrDefault(x => x.Fecha == fecha)?.Total ?? 0;
                    var etiqueta = cultura.TextInfo.ToTitleCase(fecha.ToString("ddd", cultura));
                    return new BarChartItem(etiqueta, total);
                })
                .ToList();

            var usuariosRecientesRaw = await _context.Usuarios
                .OrderByDescending(u => u.creado_el)
                .Take(5)
                .Select(u => new { u.id, u.nombre, u.apellido, u.correo, u.rol, u.creado_el, u.activo })
                .ToListAsync();

            var usuariosRecientes = usuariosRecientesRaw
                .Select(u => new AdminUserSummary(
                    u.id,
                    string.Join(" ", new[] { u.nombre, u.apellido }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                    u.correo,
                    u.rol,
                    u.creado_el,
                    u.activo))
                .ToList();

            var actividades = usuariosRecientes
                .Select(u => new AdminActivityItem(
                    Icono: ObtenerIcono(u.Rol),
                    Titulo: string.IsNullOrWhiteSpace(u.NombreCompleto) ? u.Correo : u.NombreCompleto,
                    Detalle: $"Nuevo {u.Rol.ToLower(cultura)} registrado",
                    FechaEvento: u.FechaRegistro))
                .ToList();

            var viewModel = new AdminDashboardViewModel
            {
                TotalUsuarios = totalUsuarios,
                TotalDoctores = totalDoctores,
                TotalPacientes = totalPacientes,
                CitasHoy = citasHoy,
                NuevosUsuariosSemana = nuevosUsuariosSemana,
                CitasSemana = citasSemana,
                CitasSemanaAnterior = citasSemanaAnterior,
                VolumenCitasSemanal = barras,
                UsuariosRecientes = usuariosRecientes,
                ActividadesRecientes = actividades
            };

            return View(viewModel);

            static string ObtenerIcono(string rol) => rol switch
            {
                Roles.Administrador => "admin_panel_settings",
                Roles.Doctor => "medical_services",
                Roles.Paciente => "personal_injury",
                _ => "info"
            };
        }

        // Vistas prototipo: Gestión de usuarios y Especialidades (sin lógica)
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _context.Usuarios
                .OrderByDescending(u => u.creado_el)
                .Select(u => new AdminUserRow(
                    u.id,
                    string.Join(" ", new[] { u.nombre, u.apellido }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                    u.correo,
                    u.rol,
                    u.creado_el,
                    u.activo))
                .ToListAsync();

            var viewModel = new AdminUserManagementViewModel
            {
                TotalUsuarios = await _context.Usuarios.CountAsync(),
                TotalDoctores = await _context.Usuarios.CountAsync(u => u.rol == Roles.Doctor),
                TotalPacientes = await _context.Usuarios.CountAsync(u => u.rol == Roles.Paciente),
                Usuarios = usuarios
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Especialidades()
        {
            var especialidades = await _context.Especialidades
                .Include(e => e.Doctores)
                .OrderBy(e => e.nombre)
                .Select(e => new AdminSpecialtyItem(
                    e.id,
                    e.nombre,
                    e.descripcion,
                    e.activa,
                    e.Doctores!.Count))
                .ToListAsync();

            var viewModel = new AdminSpecialtiesViewModel
            {
                TotalEspecialidades = especialidades.Count,
                EspecialidadesActivas = especialidades.Count(e => e.Activa),
                Especialidades = especialidades
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> PromoteToDoctor(long id)
        {
            var usuario = await _context.Usuarios
                .Where(u => u.id == id && u.rol == Roles.Paciente)
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado o ya es doctor.";
                return RedirectToAction(nameof(Usuarios));
            }

            var especialidades = await _context.Especialidades
                .Where(e => e.activa)
                .OrderBy(e => e.nombre)
                .ToListAsync();

            var viewModel = new PromoteToDoctorViewModel
            {
                usuario_id = usuario.id,
                nombre = usuario.nombre,
                apellido = usuario.apellido ?? "",
                usuario_nombre = usuario.nombre,
                usuario_correo = usuario.correo
            };

            ViewBag.Especialidades = especialidades;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToDoctor(PromoteToDoctorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var especialidades = await _context.Especialidades
                    .Where(e => e.activa)
                    .OrderBy(e => e.nombre)
                    .ToListAsync();
                ViewBag.Especialidades = especialidades;
                return View(model);
            }

            var usuario = await _context.Usuarios
                .Where(u => u.id == model.usuario_id && u.rol == Roles.Paciente)
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                ModelState.AddModelError("", "Usuario no encontrado o ya es doctor.");
                return View(model);
            }

            // Check if doctor with this license already exists
            var existingDoctor = await _context.Doctores
                .Where(d => d.licencia == model.licencia)
                .FirstOrDefaultAsync();

            if (existingDoctor != null)
            {
                ModelState.AddModelError("licencia", "Ya existe un doctor con esta licencia profesional.");
                var especialidades = await _context.Especialidades
                    .Where(e => e.activa)
                    .OrderBy(e => e.nombre)
                    .ToListAsync();
                ViewBag.Especialidades = especialidades;
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Upload doctor photo
                var imageResult = await _imageStorageService.SaveUserAvatarAsync(model.foto, null);
                
                if (!imageResult.Success)
                {
                    ModelState.AddModelError("foto", "Error al subir la imagen: " + imageResult.ErrorMessage);
                    var especialidades = await _context.Especialidades
                        .Where(e => e.activa)
                        .OrderBy(e => e.nombre)
                        .ToListAsync();
                    ViewBag.Especialidades = especialidades;
                    return View(model);
                }

                // Update user role and photo
                usuario.rol = Roles.Doctor;
                usuario.foto_url = imageResult.FilePath;
                usuario.nombre = model.nombre.Trim();
                usuario.apellido = model.apellido.Trim();
                usuario.segundo_apellido = string.IsNullOrWhiteSpace(model.segundo_apellido) ? null : model.segundo_apellido.Trim();

                // Create doctor profile
                var doctor = new Doctor
                {
                    usuario_id = usuario.id,
                    nombre = usuario.nombre,
                    apellido = usuario.apellido,
                    segundo_apellido = usuario.segundo_apellido,
                    licencia = model.licencia,
                    consultorio = model.consultorio,
                    biografia = model.biografia,
                    especialidad_id = model.especialidad_id
                };

                _context.Doctores.Add(doctor);

                // Remove patient profile if exists
                var paciente = await _context.Pacientes
                    .Where(p => p.usuario_id == usuario.id)
                    .FirstOrDefaultAsync();

                if (paciente != null)
                {
                    _context.Pacientes.Remove(paciente);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send notification
                await _notificationService.NotificarPromovcionDoctorAsync(usuario.id, model.especialidad_id);

                TempData["Success"] = $"Usuario {usuario.nombre} promovido a doctor exitosamente.";
                return RedirectToAction(nameof(Usuarios));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                
                // Clean up uploaded image if something failed
                if (!string.IsNullOrEmpty(model.foto?.FileName))
                {
                    // Image cleanup will be handled by the service
                }

                ModelState.AddModelError("", "Error al promover el usuario a doctor. Inténtelo de nuevo.");
                var especialidadesError = await _context.Especialidades
                    .Where(e => e.activa)
                    .OrderBy(e => e.nombre)
                    .ToListAsync();
                ViewBag.Especialidades = especialidadesError;
                return View(model);
            }
        }

        // CALENDARIO Y GESTIÓN DE CITAS PARA ADMINISTRADOR
        [HttpGet]
        public async Task<IActionResult> Calendario()
        {
            ViewBag.CurrentAdmin = await ObtenerAdministradorActual();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCitasJson()
        {
            try
            {
                var citas = await _context.Citas
                    .Include(c => c.Paciente)
                    .ThenInclude(p => p.Usuario)
                    .Include(c => c.Doctor)
                    .ThenInclude(d => d.Usuario)
                    .Include(c => c.Doctor.Especialidad)
                    .Where(c => c.fecha >= DateTime.Today.AddMonths(-1) && 
                               c.fecha <= DateTime.Today.AddMonths(2))
                    .Select(c => new
                    {
                        id = c.id,
                        title = $"Dr. {c.Doctor.Usuario.nombre} - {c.Paciente.nombre} {c.Paciente.apellido}",
                        start = c.fecha.Add(c.hora).ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = c.fecha.Add(c.hora).AddMinutes(c.duracion_minutos).ToString("yyyy-MM-ddTHH:mm:ss"),
                        color = GetColorByEstado(c.estado),
                        extendedProps = new
                        {
                            estado = c.estado,
                            motivo = c.motivo,
                            doctorNombre = $"Dr. {c.Doctor.Usuario.nombre}",
                            pacienteNombre = $"{c.Paciente.nombre} {c.Paciente.apellido}",
                            especialidad = c.Doctor.Especialidad != null ? c.Doctor.Especialidad.nombre : "General"
                        }
                    })
                    .ToListAsync();

                return Json(citas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener citas para calendario del administrador");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCitaStatusAdmin(long citaId, [FromBody] UpdateCitaStatusModel model)
        {
            try
            {
                var cita = await _context.Citas
                    .Include(c => c.Paciente)
                    .Include(c => c.Doctor)
                    .FirstOrDefaultAsync(c => c.id == citaId);

                if (cita == null)
                {
                    return Json(new { success = false, message = "Cita no encontrada" });
                }

                var estadoAnterior = cita.estado;
                cita.estado = model.Estado;
                cita.actualizada_el = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Notificar al paciente si es necesario
                if (model.Estado == "CANCELADA")
                {
                    await _notificationService.NotificarCitaCanceladaAsync(
                        cita.doctor_id,
                        cita.paciente_id,
                        cita.fecha.Add(cita.hora),
                        model.Motivo ?? "Cancelada por administración"
                    );
                }

                return Json(new { success = true, message = "Estado actualizado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado de cita desde admin");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCitaAdmin(long citaId)
        {
            try
            {
                var cita = await _context.Citas
                    .Include(c => c.Paciente)
                    .Include(c => c.Doctor)
                    .FirstOrDefaultAsync(c => c.id == citaId);

                if (cita == null)
                {
                    return Json(new { success = false, message = "Cita no encontrada" });
                }

                // Notificar a doctor y paciente
                await _notificationService.NotificarCitaCanceladaAsync(
                    cita.doctor_id,
                    cita.paciente_id,
                    cita.fecha.Add(cita.hora),
                    "Cita eliminada por administración"
                );

                _context.Citas.Remove(cita);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cita eliminada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cita desde admin");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(long usuarioId)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                usuario.activo = !usuario.activo;
                // Note: Usuario model doesn't have actualizada_el field, using existing fields

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Usuario {(usuario.activo ? "activado" : "desactivado")} correctamente",
                    newStatus = usuario.activo 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de usuario");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var fechaInicio = DateTime.Today.AddDays(-7);
                var fechaFin = DateTime.Today.AddDays(1);

                var stats = new
                {
                    citasHoy = await _context.Citas.CountAsync(c => c.fecha.Date == DateTime.Today),
                    citasSemana = await _context.Citas.CountAsync(c => c.fecha >= fechaInicio && c.fecha < fechaFin),
                    nuevosUsuarios = await _context.Usuarios.CountAsync(u => u.creado_el >= fechaInicio),
                    doctoresActivos = await _context.Doctores.CountAsync(d => d.Usuario.activo),
                    pacientesTotal = await _context.Pacientes.CountAsync(),
                    especialidadesTotal = await _context.Especialidades.CountAsync()
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas del dashboard");
                return Json(new { error = "Error al cargar estadísticas" });
            }
        }

        // Obtener notificaciones del administrador
        [HttpGet]
        public async Task<IActionResult> GetNotificaciones()
        {
            try
            {
                // Obtener las notificaciones más recientes del sistema
                var notificaciones = await _context.Notificaciones
                    .OrderByDescending(n => n.enviada_el)
                    .Take(50)
                    .Select(n => new
                    {
                        id = n.id,
                        asunto = n.asunto,
                        mensaje = n.mensaje,
                        leida = n.leida,
                        fecha = n.enviada_el.ToString("dd/MM/yyyy HH:mm")
                    })
                    .ToListAsync();

                return Json(notificaciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener notificaciones del administrador");
                return Json(new List<object>());
            }
        }

        // Marcar notificación como leída
        [HttpPost]
        public async Task<IActionResult> MarcarNotificacionLeida(long notificacionId)
        {
            try
            {
                var notificacion = await _context.Notificaciones.FindAsync(notificacionId);
                if (notificacion != null)
                {
                    notificacion.leida = true;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Notificación no encontrada" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar notificación como leída");
                return Json(new { success = false, message = "Error interno" });
            }
        }

        // MÉTODOS AUXILIARES
        private string GetColorByEstado(string estado)
        {
            return estado?.ToUpperInvariant() switch
            {
                "PENDIENTE" => "#f59e0b",      // Amber
                "CONFIRMADA" => "#10b981",     // Emerald
                "COMPLETADA" => "#3b82f6",     // Blue
                "CANCELADA" => "#ef4444",      // Red
                _ => "#6b7280"                 // Gray
            };
        }

        private async Task<Usuario?> ObtenerAdministradorActual()
        {
            var nombreUsuario = User.Identity?.Name;
            if (string.IsNullOrEmpty(nombreUsuario))
                return null;

            return await _context.Usuarios
                .FirstOrDefaultAsync(u => u.usuario == nombreUsuario && u.rol == "Administrador");
        }

        // RESPALDOS DE BASE DE DATOS
        [HttpGet]
        public async Task<IActionResult> Backups()
        {
            var config = await _backupService.GetOrCreateConfigurationAsync();
            var history = await _backupService.GetHistoryAsync(50);

            var viewModel = new DatabaseBackupPageViewModel
            {
                Config = DatabaseBackupConfigViewModel.FromEntity(config),
                History = history
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarBackupConfig(DatabaseBackupConfigViewModel model)
        {
            if (!TimeSpan.TryParse(model.AutoBackupTime, out var parsedTime))
            {
                ModelState.AddModelError(nameof(model.AutoBackupTime), "La hora debe tener el formato HH:mm");
            }

            if (!ModelState.IsValid)
            {
                var history = await _backupService.GetHistoryAsync(50);
                return View("Backups", new DatabaseBackupPageViewModel
                {
                    Config = model,
                    History = history,
                    LastError = "Revisa la información introducida."
                });
            }

            var config = await _backupService.GetOrCreateConfigurationAsync();
            config.backup_directory = model.BackupDirectory;
            config.auto_backup_enabled = model.AutoBackupEnabled;
            config.auto_backup_time = parsedTime;
            config.retention_days = model.RetentionDays;

            await _backupService.UpdateConfigurationAsync(config);
            TempData["SuccessMessage"] = "Configuración de respaldos actualizada correctamente.";
            return RedirectToAction(nameof(Backups));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EjecutarRespaldoManual()
        {
            var resultado = await _backupService.CreateBackupAsync(false);

            if (resultado.Success)
            {
                TempData["SuccessMessage"] = $"Respaldo generado en: {resultado.FileName}";
            }
            else
            {
                TempData["ErrorMessage"] = resultado.Message;
            }

            return RedirectToAction(nameof(Backups));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestaurarBackup(IFormFile? archivoRespaldo)
        {
            if (archivoRespaldo == null || archivoRespaldo.Length == 0)
            {
                TempData["ErrorMessage"] = "Selecciona un archivo .bak para restaurar.";
                return RedirectToAction(nameof(Backups));
            }

            var config = await _backupService.GetOrCreateConfigurationAsync();
            var destino = Path.Combine(config.backup_directory, $"restore_{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(archivoRespaldo.FileName)}");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
                await using (var stream = System.IO.File.Create(destino))
                {
                    await archivoRespaldo.CopyToAsync(stream);
                }

                var resultado = await _backupService.RestoreAsync(destino);

                if (resultado.Success)
                {
                    TempData["SuccessMessage"] = "La base de datos se restauró correctamente. Se recomienda reiniciar la aplicación.";
                }
                else
                {
                    TempData["ErrorMessage"] = resultado.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restaurar la base de datos desde el archivo cargado.");
                TempData["ErrorMessage"] = "Ocurrió un error al procesar el archivo de respaldo.";
            }

            return RedirectToAction(nameof(Backups));
        }

        [HttpGet]
        public IActionResult ListarDirectorios(string? path = null)
        {
            try
            {
                var basePath = string.IsNullOrWhiteSpace(path)
                    ? Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? "C:\\"
                    : path;

                var normalized = Path.GetFullPath(basePath);
                var parent = Directory.GetParent(normalized)?.FullName;

                var directories = new List<object>();
                if (Directory.Exists(normalized))
                {
                    directories = Directory
                        .GetDirectories(normalized)
                        .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                        .Select(d => (object)new
                        {
                            name = string.IsNullOrWhiteSpace(Path.GetFileName(d)) ? d : Path.GetFileName(d),
                            path = d
                        })
                        .ToList();
                }

                return Json(new
                {
                    success = true,
                    currentPath = normalized,
                    parentPath = parent,
                    directories
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al listar directorios para la ruta {Path}", path);
                return Json(new { success = false, message = "No se pudo acceder a la carpeta solicitada. Verifica permisos o intenta con otra ruta." });
            }
        }

        [HttpGet]
        public IActionResult ValidarCarpeta(string? path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return Json(new { success = false, message = "Selecciona una carpeta válida." });
                }

                var normalized = Path.GetFullPath(path);
                Directory.CreateDirectory(normalized);

                return Json(new { success = true, path = normalized });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al validar la ruta de respaldo {Path}", path);
                return Json(new { success = false, message = "No se pudo validar la carpeta. Asegurate de que la ruta sea accesible por el servidor." });
            }
        }
    }
}

// MODELOS PARA ADMINISTRACIÓN
public class UpdateCitaStatusModel
{
    public string Estado { get; set; } = "";
    public string? Motivo { get; set; }
}

