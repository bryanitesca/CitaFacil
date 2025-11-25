using System;
using System.Linq;
using System.Threading.Tasks;
using CitaFacil.Constants;
using CitaFacil.Data;
using CitaFacil.Extensions;
using CitaFacil.Models;
using CitaFacil.Services.Usuarios;
using CitaFacil.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Necesario para obtener el ID del usuario
using Microsoft.Extensions.Logging;
using CitaFacil.Services.Notifications;
using CitaFacil.Services.Notifications.Models;

namespace CitaFacil.Controllers
{
    // ¡MUY IMPORTANTE! Solo los Doctores pueden entrar aquí
    [Authorize(Roles = Roles.Doctor)]
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DoctorController> _logger;
        private readonly INotificationService _notificationService;

        public DoctorController(ApplicationDbContext context, ILogger<DoctorController> logger, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        // --- ACCIÓN 1: EL DASHBOARD PRINCIPAL (TUS CITAS DE HOY) ---
        // GET: /Doctor/Index
        public async Task<IActionResult> Index(long? citaId = null)
        {
            var viewModel = new DoctorDashboardViewModel();

            // 1. Obtener el ID del Usuario (desde la cookie de sesión)
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("No se pudo obtener el identificador del usuario autenticado.");
                return Challenge();
            }

            // 2. Encontrar el perfil de Doctor de este Usuario
            var doctor = await _context.Doctores.FirstOrDefaultAsync(d => d.usuario_id == userId);
            if (doctor == null)
            {
                _logger.LogWarning("No se encontró un perfil de doctor para el usuario {UsuarioId}", userId);
                return RedirectToAction("Index", "Home");
            }

            // 3. Buscar las citas de HOY para ESTE doctor (excluir completadas)
            var hoy = DateTime.Today;
            viewModel.CitasDeHoy = await _context.Citas
                .Where(c => c.doctor_id == doctor.id && c.fecha.Date == hoy && c.estado != "COMPLETADA" && c.Paciente != null && c.Paciente.Usuario != null && c.Paciente.Usuario.activo)
                .Include(c => c.Paciente)
                    .ThenInclude(p => p.Usuario)
                .OrderBy(c => c.hora)
                .ToListAsync();

            // Si no hay citas hoy, mostrar las próximas citas (para demo)
            if (!viewModel.CitasDeHoy.Any())
            {
                viewModel.CitasDeHoy = await _context.Citas
                    .Where(c => c.doctor_id == doctor.id && c.fecha >= hoy && c.estado != "COMPLETADA" && c.estado != "CANCELADA" && c.Paciente != null && c.Paciente.Usuario != null && c.Paciente.Usuario.activo)
                    .Include(c => c.Paciente)
                        .ThenInclude(p => p.Usuario)
                    .OrderBy(c => c.fecha)
                    .ThenBy(c => c.hora)
                    .Take(5)
                    .ToListAsync();
            }

            // 4. (Demo) Seleccionar el primer paciente de la lista para mostrar sus detalles
            if (viewModel.CitasDeHoy.Any())
            {
                viewModel.CitaSeleccionada = citaId.HasValue
                    ? viewModel.CitasDeHoy.FirstOrDefault(c => c.id == citaId.Value) ?? viewModel.CitasDeHoy.First()
                    : viewModel.CitasDeHoy.First();
                viewModel.PacienteSeleccionado = viewModel.CitaSeleccionada.Paciente;

                if (viewModel.CitaSeleccionada is not null)
                {
                    var historialPaciente = await _context.Citas
                        .Where(c => c.paciente_id == viewModel.CitaSeleccionada.paciente_id)
                        .OrderByDescending(c => c.fecha)
                        .ThenByDescending(c => c.hora)
                        .AsNoTracking()
                        .Take(10)
                        .ToListAsync();

                    viewModel.HistorialPaciente = historialPaciente;
                    viewModel.NotasPaciente = historialPaciente
                        .Where(c => !string.IsNullOrWhiteSpace(c.notas))
                        .Take(10)
                        .ToList();
                }
            }

            return View(viewModel);
        }

        // Búsqueda de pacientes para el dashboard
        [HttpGet]
        public async Task<IActionResult> BuscarPacientes(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
            {
                return Json(new List<object>());
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Json(new List<object>());
            }

            var doctor = await _context.Doctores.FirstOrDefaultAsync(d => d.usuario_id == userId);
            if (doctor == null)
            {
                return Json(new List<object>());
            }

            // Buscar pacientes que tienen citas con este doctor
            var pacientes = await _context.Citas
                .Where(c => c.doctor_id == doctor.id && c.Paciente != null && c.Paciente.Usuario != null && c.Paciente.Usuario.activo)
                .Include(c => c.Paciente)
                    .ThenInclude(p => p.Usuario)
                .Where(c => c.Paciente.nombre.Contains(termino) || 
                           c.Paciente.apellido.Contains(termino) ||
                           c.Paciente.Usuario.correo.Contains(termino))
                .Select(c => c.Paciente)
                .Distinct()
                .Take(10)
                .ToListAsync();

            var resultado = pacientes.Select(p => new
            {
                id = p.id,
                nombre = $"{p.nombre} {p.apellido}",
                correo = p.Usuario.correo,
                telefono = p.Usuario.celular
            });

            return Json(resultado);
        }

        // --- ACCIÓN 2: LISTA DE "TODOS MIS PACIENTES" ---
        // GET: /Doctor/Pacientes
        public async Task<IActionResult> Pacientes(string? busqueda = null, string? estado = null, int pagina = 1)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("No se pudo obtener el identificador del usuario autenticado.");
                return Challenge();
            }

            var doctor = await _context.Doctores.FirstOrDefaultAsync(d => d.usuario_id == userId);

            if (doctor == null)
            {
                _logger.LogWarning("No se encontró un perfil de doctor para el usuario {UsuarioId}", userId);
                return RedirectToAction("Index", "Home");
            }

            var citas = await _context.Citas
                .Where(c => c.doctor_id == doctor.id && c.Paciente != null && c.Paciente.Usuario != null && c.Paciente.Usuario.activo)
                .Include(c => c.Paciente)
                    .ThenInclude(p => p!.Usuario)
                .AsNoTracking()
                .ToListAsync();

            var pacientes = citas
                .Select(c => c.Paciente)
                .Where(p => p is not null && p.Usuario != null && p.Usuario.activo)
                .Select(p => p!)
                .DistinctBy(p => p.id)
                .ToList();

            var hoy = DateTime.Today;
            var ahora = DateTime.Now.TimeOfDay;
            var todasLasFilas = pacientes
                .Select(paciente =>
                {
                    var citasPaciente = citas
                        .Where(c => c.paciente_id == paciente.id)
                        .OrderBy(c => c.fecha)
                        .ThenBy(c => c.hora)
                        .ToList();

                    var proxima = citasPaciente
                        .FirstOrDefault(c => c.fecha.Date > hoy || (c.fecha.Date == hoy && c.hora >= ahora));

                    DateTime? proximaCita = null;
                    string proximaEstadoCodigo = string.Empty;
                    string proximaEstado = "Sin cita próxima";

                    if (proxima is not null)
                    {
                        proximaCita = DateTime.SpecifyKind(proxima.fecha.Date + proxima.hora, DateTimeKind.Unspecified);
                        proximaEstadoCodigo = (proxima.estado ?? string.Empty).Trim().ToUpperInvariant();
                        proximaEstado = TraducirEstado(proximaEstadoCodigo);
                    }

                    var nombreCompleto = ConstruirNombreCompleto(paciente);
                    var avatarIniciales = GenerarIniciales(nombreCompleto);
                    var correo = paciente.Usuario?.correo;
                    var telefono = paciente.Usuario?.celular;

                    return new DoctorPatientsViewModel.DoctorPatientRow
                    {
                        PacienteId = paciente.id,
                        NombreCompleto = nombreCompleto,
                        AvatarIniciales = avatarIniciales,
                        Correo = correo,
                        Telefono = telefono,
                        ProximaCita = proximaCita,
                        ProximaCitaEstadoCodigo = proximaEstadoCodigo,
                        ProximaCitaEstado = proximaEstado
                    };
                })
                .OrderBy(f => f.NombreCompleto)
                .ToList();

            var filasFiltradas = FiltrarPacientes(todasLasFilas, busqueda, estado);

            const int pageSize = 10;
            var totalFiltrados = filasFiltradas.Count;
            var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalFiltrados / (double)pageSize));
            pagina = Math.Clamp(pagina <= 0 ? 1 : pagina, 1, totalPaginas);

            var pacientesPagina = filasFiltradas
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new DoctorPatientsViewModel
            {
                TotalPacientes = todasLasFilas.Count,
                PacientesConfirmados = todasLasFilas.Count(f => f.ProximaCitaEstadoCodigo == "CONFIRMADA"),
                PacientesPendientes = todasLasFilas.Count(f => f.ProximaCitaEstadoCodigo is "PENDIENTE" or "REPROGRAMADA"),
                PacientesSinSeguimiento = todasLasFilas.Count(f => f.ProximaCita is null),
                Pacientes = pacientesPagina,
                TotalFiltrados = totalFiltrados,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                Busqueda = busqueda?.Trim() ?? string.Empty,
                EstadoFiltro = string.IsNullOrWhiteSpace(estado) ? "TODOS" : estado.Trim().ToUpperInvariant()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Expediente(long id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("No se pudo obtener el identificador del usuario autenticado.");
                return Challenge();
            }

            var doctor = await _context.Doctores.FirstOrDefaultAsync(d => d.usuario_id == userId);
            if (doctor == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var paciente = await ObtenerPacienteParaDoctorAsync(doctor.id, id);
            if (paciente == null || paciente.Usuario == null)
            {
                return NotFound();
            }

            var citasPaciente = await _context.Citas
                .Where(c => c.doctor_id == doctor.id && c.paciente_id == id)
                .Include(c => c.Doctor)
                    .ThenInclude(d => d.Usuario)
                .Include(c => c.Paciente)
                    .ThenInclude(p => p.Usuario)
                .AsNoTracking()
                .OrderByDescending(c => c.fecha)
                .ThenByDescending(c => c.hora)
                .ToListAsync();

            var nombreCompleto = ConstruirNombreCompleto(paciente);

            var viewModel = new DoctorPatientDetailViewModel
            {
                PacienteId = paciente.id,
                NombreCompleto = nombreCompleto,
                AvatarIniciales = GenerarIniciales(nombreCompleto),
                Correo = paciente.Usuario?.correo,
                Telefono = paciente.Usuario?.celular,
                FechaNacimiento = paciente.fecha_nacimiento,
                Genero = paciente.genero,
                Direccion = paciente.direccion,
                Paciente = paciente,
                Usuario = paciente.Usuario!,
                Historial = citasPaciente,
                TotalCitas = citasPaciente.Count,
                CitasPendientes = citasPaciente.Count(c => string.Equals(c.estado, "PENDIENTE", StringComparison.OrdinalIgnoreCase) || string.Equals(c.estado, "REPROGRAMADA", StringComparison.OrdinalIgnoreCase)),
                CitasConfirmadas = citasPaciente.Count(c => string.Equals(c.estado, "CONFIRMADA", StringComparison.OrdinalIgnoreCase)),
                CitasCompletadas = citasPaciente.Count(c => string.Equals(c.estado, "COMPLETADA", StringComparison.OrdinalIgnoreCase)),
                CitasCanceladas = citasPaciente.Count(c => string.Equals(c.estado, "CANCELADA", StringComparison.OrdinalIgnoreCase))
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Contacto(long id)
        {
            var viewModel = await ConstruirContactoViewModelAsync(id);
            if (viewModel == null)
            {
                return NotFound();
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contacto(long id, DoctorPatientContactForm formulario)
        {
            var relacion = await ObtenerDoctorPacienteAsync(id);
            if (relacion is null)
            {
                return NotFound();
            }

            var (doctor, paciente) = relacion.Value;

            if (!ModelState.IsValid)
            {
                var viewModelInvalid = await ConstruirContactoViewModelAsync(id);
                if (viewModelInvalid == null)
                {
                    return NotFound();
                }

                viewModelInvalid.Formulario = formulario;
                return View(viewModelInvalid);
            }

            var solicitud = new NotificationRequest(formulario.Asunto, formulario.Mensaje);
            var resultado = await _notificationService.RegistrarContactoPacienteAsync(doctor.id, paciente.id, solicitud);

            TempData["SuccessMessage"] = resultado.Mensaje;

            return RedirectToAction(nameof(Contacto), new { id });
        }

        private List<DoctorPatientsViewModel.DoctorPatientRow> FiltrarPacientes(List<DoctorPatientsViewModel.DoctorPatientRow> pacientes, string? busqueda, string? estado)
        {
            IEnumerable<DoctorPatientsViewModel.DoctorPatientRow> resultado = pacientes;

            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estadoNormalizado = estado.Trim().ToUpperInvariant();
                if (estadoNormalizado == "SIN_SEGUIMIENTO")
                {
                    resultado = resultado.Where(p => p.ProximaCita is null);
                }
                else if (estadoNormalizado is not ("TODOS" or ""))
                {
                    resultado = resultado.Where(p => string.Equals(p.ProximaCitaEstadoCodigo, estadoNormalizado, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var termino = busqueda.Trim();
                resultado = resultado.Where(p => CoincideBusqueda(p, termino));
            }

            return resultado.ToList();
        }

        private static bool CoincideBusqueda(DoctorPatientsViewModel.DoctorPatientRow paciente, string termino)
        {
            var comparador = StringComparison.OrdinalIgnoreCase;

            return (!string.IsNullOrWhiteSpace(paciente.NombreCompleto) && paciente.NombreCompleto.Contains(termino, comparador))
                   || (!string.IsNullOrWhiteSpace(paciente.Correo) && paciente.Correo!.Contains(termino, comparador))
                   || (!string.IsNullOrWhiteSpace(paciente.Telefono) && paciente.Telefono!.Contains(termino, comparador))
                   || paciente.PacienteId.ToString().Contains(termino, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<(Doctor doctor, Paciente paciente)?> ObtenerDoctorPacienteAsync(long pacienteId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            var doctor = await _context.Doctores.FirstOrDefaultAsync(d => d.usuario_id == userId);
            if (doctor == null)
            {
                return null;
            }

            var paciente = await ObtenerPacienteParaDoctorAsync(doctor.id, pacienteId);
            if (paciente == null)
            {
                return null;
            }

            return (doctor, paciente);
        }

        private async Task<Paciente?> ObtenerPacienteParaDoctorAsync(long doctorId, long pacienteId)
        {
            var paciente = await _context.Pacientes
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.id == pacienteId);

            if (paciente == null || paciente.Usuario == null || !paciente.Usuario.activo)
            {
                return null;
            }

            var tieneRelacion = await _context.Citas.AnyAsync(c => c.doctor_id == doctorId && c.paciente_id == pacienteId);
            return tieneRelacion ? paciente : null;
        }

        private async Task<DoctorPatientContactViewModel?> ConstruirContactoViewModelAsync(long pacienteId)
        {
            var relacion = await ObtenerDoctorPacienteAsync(pacienteId);
            if (relacion is null)
            {
                return null;
            }

            var (doctor, paciente) = relacion.Value;
            if (paciente.Usuario == null)
            {
                return null;
            }

            var nombreCompleto = ConstruirNombreCompleto(paciente);
            var historial = await _notificationService.ObtenerHistorialAsync(doctor.id, paciente.id);

            return new DoctorPatientContactViewModel
            {
                PacienteId = paciente.id,
                NombreCompleto = nombreCompleto,
                AvatarIniciales = GenerarIniciales(nombreCompleto),
                Correo = paciente.Usuario?.correo,
                Telefono = paciente.Usuario?.celular,
                Formulario = new DoctorPatientContactForm(),
                Historial = historial
            };
        }

        private static string ConstruirNombreCompleto(Paciente paciente)
        {
            var partes = new[] { paciente.nombre, paciente.apellido, paciente.segundo_apellido }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim());

            var nombre = string.Join(" ", partes);
            return string.IsNullOrWhiteSpace(nombre) ? "Paciente" : nombre;
        }

        private static string GenerarIniciales(string nombreCompleto)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                return "PA";
            }

            var palabras = nombreCompleto
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (palabras.Length == 1)
            {
                return palabras[0].Length >= 2
                    ? palabras[0][0..2].ToUpperInvariant()
                    : palabras[0][0].ToString().ToUpperInvariant();
            }

            return string.Concat(palabras[0][0], palabras[^1][0]).ToUpperInvariant();
        }

        private static string TraducirEstado(string? estadoCodigo)
        {
            if (string.IsNullOrWhiteSpace(estadoCodigo))
            {
                return "Sin cita próxima";
            }

            return estadoCodigo.ToUpperInvariant() switch
            {
                "CONFIRMADA" => "Confirmada",
                "PENDIENTE" => "Pendiente",
                "INICIADA" => "En consulta",
                "REPROGRAMADA" => "Reprogramación pendiente",
                "CANCELADA" => "Cancelada",
                "COMPLETADA" => "Completada",
                _ => System.Globalization.CultureInfo.GetCultureInfo("es-MX").TextInfo.ToTitleCase(estadoCodigo.ToLowerInvariant())
            };
        }

        private static string ObtenerColorPorEstado(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
            {
                return "#6b7280";
            }

            return estado.ToUpperInvariant() switch
            {
                "PENDIENTE" => "#f59e0b",
                "CONFIRMADA" => "#10b981",
                "INICIADA" => "#2563eb",
                "COMPLETADA" => "#3b82f6",
                "CANCELADA" => "#ef4444",
                _ => "#6b7280"
            };
        }

        // --- ACCIÓN 3: RECOMENDACIÓN DE CALENDARIO ---
        // GET: /Doctor/Calendario
        public IActionResult Calendario()
        {
            // Esta vista solo necesita cargar el layout.
            // El calendario se cargará con JavaScript (ver recomendación)
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCitasJson()
        {
            try
            {
                // 1. Obtener el ID del Usuario (desde la cookie de sesión)
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!long.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("No se pudo obtener el identificador del usuario autenticado.");
                    return Challenge();
                }

                // 2. Encontrar el perfil de Doctor de este Usuario
                var doctor = await _context.Doctores.FirstOrDefaultAsync(d => d.usuario_id == userId);
                if (doctor == null)
                {
                    _logger.LogWarning("No se encontró un perfil de doctor para el usuario {UsuarioId}", userId);
                    return RedirectToAction("Index", "Home");
                }

                // 3. Buscar todas las citas para ESTE doctor
                var citas = await _context.Citas
                    .Where(c => c.doctor_id == doctor.id)
                    .Include(c => c.Paciente)
                        .ThenInclude(p => p.Usuario)
                    .Include(c => c.Doctor)
                    .AsNoTracking()
                    .Where(c => c.Doctor != null && c.Paciente != null && c.Paciente.Usuario != null)
                    .ToListAsync();

                // 4. Transformar los datos al formato que FullCalendar espera
                var events = citas.Select(c => new
                {
                    id = c.id,
                    title = ConstruirNombreCompleto(c.Paciente),
                    start = c.fecha.Add(c.hora),
                    end = c.fecha.Add(c.hora).AddMinutes(c.duracion_minutos),
                    allDay = false,
                    color = ObtenerColorPorEstado(c.estado),
                    extendedProps = new
                    {
                        estado = c.estado,
                        pacienteNombre = ConstruirNombreCompleto(c.Paciente),
                        motivo = c.motivo ?? "Consulta general",
                        ubicacion = c.es_virtual
                            ? "Consulta virtual"
                            : (string.IsNullOrWhiteSpace(c.Doctor?.consultorio) ? "Consultorio por confirmar" : c.Doctor.consultorio),
                        esVirtual = c.es_virtual,
                        notas = c.notas
                    }
                });

                return new JsonResult(events);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
            }
        }

        // Vista de Informes funcional
        [HttpGet]
        public async Task<IActionResult> Informes()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("No se pudo obtener el identificador del usuario autenticado.");
                return Challenge();
            }

            var doctor = await _context.Doctores
                .Include(d => d.Usuario)
                .Include(d => d.Especialidad)
                .FirstOrDefaultAsync(d => d.usuario_id == userId);
            
            if (doctor == null)
            {
                _logger.LogWarning("No se encontró un perfil de doctor para el usuario {UsuarioId}", userId);
                return RedirectToAction("Index", "Home");
            }

            // Obtener estadísticas básicas
            var citas = await _context.Citas
                .Where(c => c.doctor_id == doctor.id)
                .Include(c => c.Paciente)
                    .ThenInclude(p => p.Usuario)
                .AsNoTracking()
                .ToListAsync();

            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);

            ViewBag.Doctor = doctor;
            ViewBag.TotalCitas = citas.Count;
            ViewBag.CitasEsteMes = citas.Count(c => c.fecha >= inicioMes && c.fecha <= finMes);
            ViewBag.CitasCompletadas = citas.Count(c => c.estado == "COMPLETADA");
            ViewBag.CitasPendientes = citas.Count(c => c.estado == "PENDIENTE");
            ViewBag.PacientesUnicos = citas.Select(c => c.paciente_id).Distinct().Count();
            
            return View();
        }

        // GET: /Doctor/Ajustes
        public async Task<IActionResult> Ajustes()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("No se pudo obtener el identificador del usuario autenticado.");
                return Challenge();
            }

            var doctor = await _context.Doctores
                .Include(d => d.Usuario)
                .Include(d => d.Especialidad)
                .FirstOrDefaultAsync(d => d.usuario_id == userId);

            if (doctor == null)
            {
                _logger.LogWarning("No se encontró el perfil de doctor para el usuario {UserId}", userId);
                return NotFound("No se encontró el perfil de doctor.");
            }

            ViewBag.Doctor = doctor;
            return View();
        }

        // POST: /Doctor/Ajustes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ajustes(string nombre, string apellido, string segundoApellido, string celular, string consultorio)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Challenge();
            }

            var doctor = await _context.Doctores
                .Include(d => d.Usuario)
                .FirstOrDefaultAsync(d => d.usuario_id == userId);

            if (doctor == null)
            {
                return NotFound();
            }

            var nombreSanitizado = string.IsNullOrWhiteSpace(nombre) ? doctor.nombre : nombre.Trim();
            var apellidoSanitizado = string.IsNullOrWhiteSpace(apellido) ? doctor.apellido : apellido.Trim();
            var segundoSanitizado = string.IsNullOrWhiteSpace(segundoApellido) ? null : segundoApellido.Trim();
            var celularSanitizado = string.IsNullOrWhiteSpace(celular) ? null : celular.Trim();
            var consultorioSanitizado = string.IsNullOrWhiteSpace(consultorio) ? null : consultorio.Trim();

            // Actualizar datos del doctor
            doctor.nombre = nombreSanitizado;
            doctor.apellido = apellidoSanitizado ?? doctor.apellido;
            doctor.segundo_apellido = segundoSanitizado;
            doctor.consultorio = consultorioSanitizado;

            // Actualizar datos del usuario
            if (doctor.Usuario != null)
            {
                doctor.Usuario.nombre = nombreSanitizado;
                doctor.Usuario.apellido = apellidoSanitizado ?? doctor.Usuario.apellido;
                doctor.Usuario.segundo_apellido = segundoSanitizado;
                doctor.Usuario.celular = celularSanitizado;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Configuración actualizada correctamente";
            return RedirectToAction(nameof(Ajustes));
        }

        // --- NOTIFICACIONES ---
        
        // GET: /Doctor/Notificaciones
        [HttpGet]
        public async Task<IActionResult> Notificaciones()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Challenge();
            }

            var doctor = await _context.Doctores
                .Include(d => d.Usuario)
                .Include(d => d.Especialidad)
                .FirstOrDefaultAsync(d => d.usuario_id == userId);
            
            if (doctor == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Doctor = doctor;
            return View();
        }

        // GET: /Doctor/ObtenerNotificaciones
        [HttpGet]
        public async Task<IActionResult> ObtenerNotificaciones()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Json(new { success = false, message = "Usuario no autenticado" });
            }

            var doctor = await _context.Doctores.FirstOrDefaultAsync(d => d.usuario_id == userId);
            if (doctor == null)
            {
                return Json(new { success = false, message = "Doctor no encontrado" });
            }

            var notificaciones = await _notificationService.ObtenerNotificacionesDoctorAsync(doctor.id);
            var noLeidas = await _notificationService.ContarNotificacionesNoLeidasDoctorAsync(doctor.id);

            return Json(new 
            { 
                success = true, 
                notificaciones = notificaciones,
                totalNoLeidas = noLeidas
            });
        }

        // POST: /Doctor/MarcarNotificacionLeida
        [HttpPost]
        public async Task<IActionResult> MarcarNotificacionLeida([FromBody] long notificacionId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Json(new { success = false, message = "Usuario no autenticado" });
            }

            var doctor = await _context.Doctores.FirstOrDefaultAsync(d => d.usuario_id == userId);
            if (doctor == null)
            {
                return Json(new { success = false, message = "Doctor no encontrado" });
            }

            var resultado = await _notificationService.MarcarNotificacionLeidaAsync(notificacionId, doctor.id);

            return Json(new { success = resultado.Exito, message = resultado.Mensaje });
        }

        // POST: /Doctor/MarcarTodasLeidas
        [HttpPost]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Json(new { success = false, message = "Usuario no autenticado" });
            }

            var doctor = await _context.Doctores.FirstOrDefaultAsync(d => d.usuario_id == userId);
            if (doctor == null)
            {
                return Json(new { success = false, message = "Doctor no encontrado" });
            }

            var resultado = await _notificationService.MarcarTodasLeidasDoctorAsync(doctor.id);

            return Json(new { success = resultado.Exito, message = resultado.Mensaje });
        }

    }
}
