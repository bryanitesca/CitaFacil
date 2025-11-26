using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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
using Microsoft.Extensions.Logging;

namespace CitaFacil.Controllers
{
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

        // ==========================================
        // DASHBOARD PRINCIPAL
        // ==========================================
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

        // ==========================================
        // GESTIÓN DE USUARIOS
        // ==========================================
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
                await _context.SaveChangesAsync();

                return Json(new
                {
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

        // ==========================================
        // GESTIÓN DE ESPECIALIDADES
        // ==========================================
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

        // ==========================================
        // PROMOVER A DOCTOR
        // ==========================================
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
                ViewBag.Especialidades = await _context.Especialidades.Where(e => e.activa).OrderBy(e => e.nombre).ToListAsync();
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

            var existingDoctor = await _context.Doctores.FirstOrDefaultAsync(d => d.licencia == model.licencia);
            if (existingDoctor != null)
            {
                ModelState.AddModelError("licencia", "Ya existe un doctor con esta licencia.");
                ViewBag.Especialidades = await _context.Especialidades.Where(e => e.activa).OrderBy(e => e.nombre).ToListAsync();
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var imageResult = await _imageStorageService.SaveUserAvatarAsync(model.foto, null);
                if (!imageResult.Success)
                {
                    ModelState.AddModelError("foto", "Error al subir la imagen: " + imageResult.ErrorMessage);
                    ViewBag.Especialidades = await _context.Especialidades.Where(e => e.activa).OrderBy(e => e.nombre).ToListAsync();
                    return View(model);
                }

                usuario.rol = Roles.Doctor;
                usuario.foto_url = imageResult.FilePath;
                usuario.nombre = model.nombre.Trim();
                usuario.apellido = model.apellido.Trim();
                usuario.segundo_apellido = string.IsNullOrWhiteSpace(model.segundo_apellido) ? null : model.segundo_apellido.Trim();

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

                var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.usuario_id == usuario.id);
                if (paciente != null) _context.Pacientes.Remove(paciente);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _notificationService.NotificarPromovcionDoctorAsync(usuario.id, model.especialidad_id);

                TempData["Success"] = $"Usuario {usuario.nombre} promovido a doctor exitosamente.";
                return RedirectToAction(nameof(Usuarios));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error al promover el usuario.");
                ViewBag.Especialidades = await _context.Especialidades.Where(e => e.activa).OrderBy(e => e.nombre).ToListAsync();
                return View(model);
            }
        }

        // ==========================================
        // CALENDARIO Y CITAS
        // ==========================================
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
                    .Include(c => c.Paciente).ThenInclude(p => p.Usuario)
                    .Include(c => c.Doctor).ThenInclude(d => d.Usuario)
                    .Include(c => c.Doctor.Especialidad)
                    .Where(c => c.fecha >= DateTime.Today.AddMonths(-1) && c.fecha <= DateTime.Today.AddMonths(2))
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
                _logger.LogError(ex, "Error obteniendo citas admin");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCitaStatusAdmin(long citaId, [FromBody] UpdateCitaStatusModel model)
        {
            try
            {
                var cita = await _context.Citas.Include(c => c.Paciente).Include(c => c.Doctor).FirstOrDefaultAsync(c => c.id == citaId);
                if (cita == null) return Json(new { success = false, message = "Cita no encontrada" });

                cita.estado = model.Estado;
                cita.actualizada_el = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                if (model.Estado == "CANCELADA")
                {
                    await _notificationService.NotificarCitaCanceladaAsync(
                        cita.doctor_id, cita.paciente_id, cita.fecha.Add(cita.hora), model.Motivo ?? "Por administración");
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando cita admin");
                return Json(new { success = false });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCitaAdmin(long citaId)
        {
            try
            {
                var cita = await _context.Citas.Include(c => c.Paciente).Include(c => c.Doctor).FirstOrDefaultAsync(c => c.id == citaId);
                if (cita == null) return Json(new { success = false });

                await _notificationService.NotificarCitaCanceladaAsync(cita.doctor_id, cita.paciente_id, cita.fecha.Add(cita.hora), "Cita eliminada por administración");
                _context.Citas.Remove(cita);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando cita admin");
                return Json(new { success = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
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

        [HttpGet]
        public async Task<IActionResult> GetNotificaciones()
        {
            var notificaciones = await _context.Notificaciones
                .OrderByDescending(n => n.enviada_el).Take(50)
                .Select(n => new { n.id, n.asunto, n.mensaje, n.leida, fecha = n.enviada_el.ToString("dd/MM/yyyy HH:mm") })
                .ToListAsync();
            return Json(notificaciones);
        }

        [HttpPost]
        public async Task<IActionResult> MarcarNotificacionLeida(long notificacionId)
        {
            var notificacion = await _context.Notificaciones.FindAsync(notificacionId);
            if (notificacion != null)
            {
                notificacion.leida = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // ==========================================
        // RESPALDOS DE BASE DE DATOS
        // ==========================================

        [HttpGet]
        // CAMBIO: Se recibe parámetro de página para la paginación
        public async Task<IActionResult> Backups(int page = 1)
        {
            const int pageSize = 5; // 5 por página como pediste

            // Recuperamos historial suficiente (ej. 100) para paginar
            var fullHistory = await _backupService.GetHistoryAsync(100);

            var totalItems = fullHistory.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Ajustar página actual a rangos válidos
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            // Paginación en memoria
            var pagedHistory = fullHistory
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new DatabaseBackupPageViewModel
            {
                Config = new DatabaseBackupConfigViewModel(),
                History = pagedHistory,
                CurrentPage = page,
                TotalPages = totalPages
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EjecutarRespaldoManual()
        {
            var resultado = await _backupService.CreateBackupAsync(false);

            if (resultado.Success)
            {
                try
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(resultado.FilePath);
                    var fileName = Path.GetFileName(resultado.FilePath);
                    return File(fileBytes, "application/octet-stream", fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al descargar el respaldo");
                    TempData["ErrorMessage"] = "Error al descargar el archivo: " + ex.Message;
                }
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
                TempData["ErrorMessage"] = "Debes seleccionar un archivo .bak válido.";
                return RedirectToAction(nameof(Backups));
            }

            var folder = Path.GetTempPath();
            var destino = Path.Combine(folder, $"restore_{DateTime.UtcNow.Ticks}_{Path.GetFileName(archivoRespaldo.FileName)}");

            try
            {
                using (var stream = new FileStream(destino, FileMode.Create))
                {
                    await archivoRespaldo.CopyToAsync(stream);
                }

                var resultado = await _backupService.RestoreAsync(destino);

                if (resultado.Success)
                {
                    TempData["SuccessMessage"] = "Base de datos restaurada correctamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = resultado.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al restaurar DB");
                TempData["ErrorMessage"] = "Ocurrió un error al procesar el archivo.";
            }
            finally
            {
                if (System.IO.File.Exists(destino))
                {
                    try { System.IO.File.Delete(destino); } catch { }
                }
            }

            return RedirectToAction(nameof(Backups));
        }

        // HELPERS
        private string GetColorByEstado(string estado) => estado?.ToUpperInvariant() switch
        {
            "PENDIENTE" => "#f59e0b",
            "CONFIRMADA" => "#10b981",
            "COMPLETADA" => "#3b82f6",
            "CANCELADA" => "#ef4444",
            _ => "#6b7280"
        };

        private async Task<Usuario?> ObtenerAdministradorActual()
        {
            var nombre = User.Identity?.Name;
            if (string.IsNullOrEmpty(nombre)) return null;
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.usuario == nombre && u.rol == Roles.Administrador);
        }
    }

    public class UpdateCitaStatusModel
    {
        public string Estado { get; set; } = "";
        public string? Motivo { get; set; }
    }
}