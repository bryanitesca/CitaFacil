using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Security.Claims;

namespace CitaFacil.Controllers
{
    [Authorize]
    public class CitasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUsuarioPerfilService _perfilService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CitasController> _logger;

        public CitasController(ApplicationDbContext context, IUsuarioPerfilService perfilService, INotificationService notificationService, ILogger<CitasController> logger)
        {
            _context = context;
            _perfilService = perfilService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: Citas/Historial - Para pacientes
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> Historial(string? busqueda = null, string? especialidad = null, DateTime? fechaInicio = null, DateTime? fechaFin = null)
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

            var query = _context.Citas
                .Include(c => c.Doctor)
                    .ThenInclude(d => d.Usuario)
                .Include(c => c.Doctor.Especialidad)
                .Where(c => c.paciente_id == paciente.id && 
                           (c.estado == "COMPLETADA" || c.fecha.Date < DateTime.Today))
                .AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query = query.Where(c => 
                    c.Doctor.Usuario.nombre.Contains(busqueda) ||
                    c.Doctor.apellido.Contains(busqueda) ||
                    c.Doctor.Especialidad.nombre.Contains(busqueda) ||
                    c.motivo.Contains(busqueda) ||
                    c.notas.Contains(busqueda));
            }

            if (!string.IsNullOrWhiteSpace(especialidad))
            {
                query = query.Where(c => c.Doctor.Especialidad.nombre == especialidad);
            }

            if (fechaInicio.HasValue)
            {
                query = query.Where(c => c.fecha >= fechaInicio.Value.Date);
            }

            if (fechaFin.HasValue)
            {
                query = query.Where(c => c.fecha <= fechaFin.Value.Date);
            }

            var citas = await query
                .OrderByDescending(c => c.fecha)
                .ThenByDescending(c => c.hora)
                .ToListAsync();

            var viewModel = new MyAppointmentsViewModel
            {
                Paciente = paciente,
                ProximasCitas = new List<Cita>(),
                CitasPasadas = citas
            };

            ViewBag.Busqueda = busqueda;
            ViewBag.EspecialidadFiltro = especialidad;
            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;

            // Obtener especialidades para filtro
            ViewBag.Especialidades = await _context.Especialidades
                .Where(e => e.activa)
                .OrderBy(e => e.nombre)
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Citas/MisCitas - Para pacientes
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> MisCitas()
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

            var citas = await _context.Citas
                .Include(c => c.Doctor)
                    .ThenInclude(d => d.Usuario)
                .Include(c => c.Doctor.Especialidad)
                .Where(c => c.paciente_id == paciente.id)
                .OrderBy(c => c.fecha)
                .ThenBy(c => c.hora)
                .ToListAsync();

            var hoy = DateTime.Today;
            var viewModel = new MyAppointmentsViewModel
            {
                Paciente = paciente,
                ProximasCitas = citas.Where(c => c.fecha.Date >= hoy && 
                                               c.estado != "COMPLETADA" && 
                                               c.estado != "CANCELADA").ToList(),
                CitasPasadas = citas.Where(c => c.estado == "COMPLETADA").OrderByDescending(c => c.fecha).ThenByDescending(c => c.hora).ToList()
            };

            return View(viewModel);
        }

        // GET: Citas
        public async Task<IActionResult> Index()
        {
            var role = User.GetRole();
            var userId = User.GetUserId();

            var query = _context.Citas
                .Include(c => c.Doctor)
                    .ThenInclude(d => d.Usuario)
                .Include(c => c.Paciente)
                    .ThenInclude(p => p.Usuario)
                .AsQueryable();

            if (role == Roles.Doctor)
            {
                if (userId is null)
                {
                    return Forbid();
                }

                var doctor = await _perfilService.ObtenerDoctorAsync(userId.Value);
                if (doctor == null)
                {
                    return Forbid();
                }

                query = query.Where(c => c.doctor_id == doctor.id);
            }
            else if (role == Roles.Paciente)
            {
                if (userId is null)
                {
                    return Forbid();
                }

                var paciente = await _perfilService.ObtenerPacienteAsync(userId.Value);
                if (paciente == null)
                {
                    return Forbid();
                }

                query = query.Where(c => c.paciente_id == paciente.id);
            }
            else if (role != Roles.Administrador)
            {
                return Forbid();
            }

            var citas = await query
                .OrderBy(c => c.fecha)
                .ThenBy(c => c.hora)
                .ToListAsync();

            return View(citas);
        }

        // GET: Citas/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = User.GetRole();
            var userId = User.GetUserId();

            var query = _context.Citas
                .Include(c => c.Doctor)
                    .ThenInclude(d => d.Usuario)
                .Include(c => c.Paciente)
                    .ThenInclude(p => p.Usuario)
                .Where(c => c.id == id);

            if (role == Roles.Doctor)
            {
                if (userId is null)
                {
                    return Forbid();
                }

                var doctor = await _perfilService.ObtenerDoctorAsync(userId.Value);

                if (doctor == null)
                {
                    return Forbid();
                }

                query = query.Where(c => c.doctor_id == doctor.id);
            }
            else if (role == Roles.Paciente)
            {
                if (userId is null)
                {
                    return Forbid();
                }

                var paciente = await _perfilService.ObtenerPacienteAsync(userId.Value);

                if (paciente == null)
                {
                    return Forbid();
                }

                query = query.Where(c => c.paciente_id == paciente.id);
            }
            else if (role != Roles.Administrador)
            {
                return Forbid();
            }

            var cita = await query.FirstOrDefaultAsync();
            if (cita == null)
            {
                return NotFound();
            }

            return View(cita);
        }

        // GET: Citas/Create
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Create()
        {
            await PopulateSelectLists();
            return View();
        }

        // POST: Citas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Create([Bind("id,fecha,hora,motivo,notas,es_virtual,duracion_minutos,estado,paciente_id,doctor_id")] Cita cita)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cita);
                await _context.SaveChangesAsync();

                // Send notification for new appointment
                var fechaCita = cita.fecha.Date.Add(cita.hora);
                await _notificationService.NotificarCitaCreadaAsync(cita.doctor_id, cita.paciente_id, fechaCita, cita.motivo ?? "Consulta médica");

                TempData["Success"] = "Cita creada exitosamente. Se ha enviado una notificación.";
                return RedirectToAction(nameof(Index));
            }
            await PopulateSelectLists(cita.doctor_id, cita.paciente_id);
            return View(cita);
        }

        // GET: Citas/Edit/5
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cita = await _context.Citas.FindAsync(id);
            if (cita == null)
            {
                return NotFound();
            }
            await PopulateSelectLists(cita.doctor_id, cita.paciente_id);
            return View(cita);
        }

        // POST: Citas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Edit(long id, [Bind("id,fecha,hora,motivo,notas,es_virtual,duracion_minutos,estado,paciente_id,doctor_id")] Cita cita)
        {
            if (id != cita.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var citaOriginal = await _context.Citas.AsNoTracking().FirstOrDefaultAsync(c => c.id == id);
                    
                    cita.actualizada_el = DateTime.UtcNow;
                    _context.Update(cita);
                    await _context.SaveChangesAsync();

                    // Enviar notificaciones si cambió fecha/hora o estado
                    var fechaCitaOriginal = citaOriginal.fecha.Add(citaOriginal.hora);
                    var fechaCitaNueva = cita.fecha.Add(cita.hora);

                    if (fechaCitaOriginal != fechaCitaNueva)
                    {
                        await _notificationService.NotificarCitaReprogramadaAsync(
                            cita.doctor_id, 
                            cita.paciente_id, 
                            fechaCitaOriginal, 
                            fechaCitaNueva, 
                            "Reprogramada por administrador");
                    }
                    else if (cita.estado == "COMPLETADA" && citaOriginal.estado != "COMPLETADA")
                    {
                        await _notificationService.NotificarCitaCompletadaAsync(cita.doctor_id, cita.paciente_id, fechaCitaNueva);
                    }
                    else if (cita.estado == "CANCELADA" && citaOriginal.estado != "CANCELADA")
                    {
                        await _notificationService.NotificarCitaCanceladaAsync(cita.doctor_id, cita.paciente_id, fechaCitaNueva, "Cancelada por administrador");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CitaExists(cita.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            await PopulateSelectLists(cita.doctor_id, cita.paciente_id);
            return View(cita);
        }

        // GET: Citas/Delete/5
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cita = await _context.Citas
                .Include(c => c.Doctor)
                    .ThenInclude(d => d.Usuario)
                .Include(c => c.Paciente)
                    .ThenInclude(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.id == id);
            if (cita == null)
            {
                return NotFound();
            }

            return View(cita);
        }

        // POST: Citas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Administrador)]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var cita = await _context.Citas.FindAsync(id);
            if (cita != null)
            {
                _context.Citas.Remove(cita);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CitaExists(long id)
        {
            return _context.Citas.Any(e => e.id == id);
        }

        private async Task PopulateSelectLists(long? doctorSeleccionado = null, long? pacienteSeleccionado = null)
        {
            var doctores = await _context.Doctores
                .Include(d => d.Usuario)
                .Where(d => d.Usuario != null && d.Usuario.activo)
                .OrderBy(d => d.Usuario.nombre)
                .Select(d => new
                {
                    d.id,
                    Nombre = string.Join(" ", new[] { d.Usuario.nombre, d.apellido }.Where(s => !string.IsNullOrWhiteSpace(s)))
                })
                .ToListAsync();

            ViewData["doctor_id"] = new SelectList(doctores, "id", "Nombre", doctorSeleccionado);

            var pacientes = await _context.Pacientes
                .Include(p => p.Usuario)
                .Where(p => p.Usuario != null && p.Usuario.activo)
                .OrderBy(p => p.Usuario.nombre)
                .Select(p => new
                {
                    p.id,
                    Nombre = string.Join(" ", new[] { p.Usuario.nombre, p.apellido }.Where(s => !string.IsNullOrWhiteSpace(s)))
                })
                .ToListAsync();

            ViewData["paciente_id"] = new SelectList(pacientes, "id", "Nombre", pacienteSeleccionado);
        }

        private static readonly IReadOnlyList<TimeSpan> HorariosLaborales = Enumerable.Range(0, 18)
            .Select(index => TimeSpan.FromMinutes((9 * 60) + index * 30))
            .ToArray();

        private static readonly HashSet<string> EstadosBloqueantes = new(new[]
        {
            "PENDIENTE",
            "CONFIRMADA",
            "REPROGRAMADA",
            "INICIADA"
        }, StringComparer.OrdinalIgnoreCase);

        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> AgendarPaso1(string? busqueda = null, long? citaId = null)
        {
            var termino = string.IsNullOrWhiteSpace(busqueda) ? null : busqueda.Trim();

            var especialidadesQuery = _context.Especialidades
                .Include(e => e.Doctores)
                    .ThenInclude(d => d.Usuario)
                .AsNoTracking()
                .Where(e => e.activa);

            if (!string.IsNullOrWhiteSpace(termino))
            {
                especialidadesQuery = especialidadesQuery.Where(e =>
                    EF.Functions.Like(e.nombre, $"%{termino}%") ||
                    (e.descripcion != null && EF.Functions.Like(e.descripcion, $"%{termino}%")));
            }

            var especialidades = await especialidadesQuery
                .OrderBy(e => e.nombre)
                .Select(e => new ScheduleSpecialtyItem(
                    e.id,
                    e.nombre,
                    e.descripcion,
                    e.activa,
                    e.Doctores!.Count(d => d.Usuario != null && d.Usuario.activo),
                    e.icono))
                .ToListAsync();

            var viewModel = new ScheduleStep1ViewModel
            {
                Busqueda = termino,
                Especialidades = especialidades,
                CitaId = citaId
            };

            return View(viewModel);
        }

        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> AgendarPaso2(long especialidadId, long? doctorId = null, DateOnly? fecha = null, TimeSpan? hora = null, long? citaId = null)
        {
            var especialidad = await _context.Especialidades
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.id == especialidadId && e.activa);

            if (especialidad is null)
            {
                TempData["ErrorMessage"] = "La especialidad seleccionada no existe o está inactiva.";
                return RedirectToAction(nameof(AgendarPaso1));
            }

            var doctores = await _context.Doctores
                .Where(d => d.especialidad_id == especialidadId && d.Usuario != null && d.Usuario.activo)
                .Include(d => d.Usuario)
                .Include(d => d.Especialidad)
                .AsNoTracking()
                .OrderBy(d => d.Usuario!.nombre)
                .ToListAsync();

            if (doctores.Count == 0)
            {
                var sinDoctores = new ScheduleStep2ViewModel
                {
                    EspecialidadId = especialidadId,
                    EspecialidadNombre = especialidad.nombre,
                    EspecialidadDescripcion = especialidad.descripcion,
                    TieneDisponibilidad = false,
                    CitaId = citaId
                };

                TempData["ErrorMessage"] = "Aún no hay doctores registrados para esta especialidad.";
                return View(sinDoctores);
            }

            var doctorSeleccionado = doctorId.HasValue
                ? doctores.FirstOrDefault(d => d.id == doctorId.Value)
                : doctores.FirstOrDefault();

            if (doctorSeleccionado is null)
            {
                doctorSeleccionado = doctores.First();
            }

            var (agendaBase, fechaSugerida, horaSugerida) = await ConstruirAgendaDoctorAsync(doctorSeleccionado.id, fecha, hora, citaId);

            DateOnly? fechaFinal = fecha;
            TimeSpan? horaFinal = hora;

            bool SeleccionDisponible(DateOnly? f, TimeSpan? h) => f.HasValue && h.HasValue && agendaBase.Any(d => d.Fecha == f.Value && d.Horas.Any(slot => slot.Hora == h.Value && slot.Disponible));

            if (!SeleccionDisponible(fechaFinal, horaFinal))
            {
                fechaFinal = fechaSugerida;
                horaFinal = horaSugerida;
            }

            // Normalizar la hora seleccionada para comparación (solo horas y minutos)
            TimeSpan? horaNormalizada = horaFinal.HasValue 
                ? new TimeSpan(horaFinal.Value.Hours, horaFinal.Value.Minutes, 0) 
                : null;

            var agenda = agendaBase
                .Select(dia => dia with
                {
                    Horas = dia.Horas
                        .Select(horaSlot => {
                            var slotNormalizado = new TimeSpan(horaSlot.Hora.Hours, horaSlot.Hora.Minutes, 0);
                            var esSeleccionado = fechaFinal.HasValue && horaNormalizada.HasValue && 
                                                dia.Fecha == fechaFinal.Value && 
                                                slotNormalizado == horaNormalizada.Value;
                            
                            return horaSlot with { Seleccionado = esSeleccionado };
                        })
                        .ToList()
                })
                .ToList();

            var doctorItems = doctores
                .Select(d => new ScheduleDoctorItem(
                    d.id,
                    ConstruirNombreCompleto(d),
                    d.Especialidad?.nombre ?? especialidad.nombre,
                    d.Usuario?.foto_url,
                    ConstruirResumenDoctor(d),
                    d.id == doctorSeleccionado!.id))
                .ToList();

            var doctorDetalle = new ScheduleDoctorDetail(
                doctorSeleccionado.id,
                ConstruirNombreCompleto(doctorSeleccionado),
                doctorSeleccionado.Usuario?.foto_url,
                doctorSeleccionado.Especialidad?.nombre ?? especialidad.nombre,
                string.IsNullOrWhiteSpace(doctorSeleccionado.biografia) ? null : doctorSeleccionado.biografia,
                string.IsNullOrWhiteSpace(doctorSeleccionado.consultorio) ? null : doctorSeleccionado.consultorio,
                doctorSeleccionado.Usuario?.celular);

            var viewModel = new ScheduleStep2ViewModel
            {
                EspecialidadId = especialidadId,
                EspecialidadNombre = especialidad.nombre,
                EspecialidadDescripcion = especialidad.descripcion,
                Doctores = doctorItems,
                DoctorSeleccionadoId = doctorSeleccionado.id,
                DoctorSeleccionado = doctorDetalle,
                Agenda = agenda,
                FechaSeleccionada = fechaFinal,
                HoraSeleccionada = horaFinal,
                TieneDisponibilidad = agenda.Any(d => d.EstaDisponible),
                CitaId = citaId
            };

            if (!viewModel.TieneDisponibilidad)
            {
                TempData["ErrorMessage"] = "No encontramos horarios disponibles en los próximos días. Intenta con otro doctor.";
            }

            return View(viewModel);
        }

        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> AgendarPaso3(long especialidadId, long doctorId, DateOnly fecha, TimeSpan hora, long? citaId = null)
        {
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Forbid();
            }

            var paciente = await _perfilService.ObtenerPacienteAsync(userId.Value);
            if (paciente is null)
            {
                return NotFound();
            }

            Cita? citaExistente = null;
            if (citaId.HasValue)
            {
                citaExistente = await _context.Citas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.id == citaId.Value && c.paciente_id == paciente.id);

                if (citaExistente is null)
                {
                    TempData["ErrorMessage"] = "No encontramos la cita que quieres reprogramar.";
                    return RedirectToAction(nameof(MisCitas));
                }
            }

            var doctor = await _context.Doctores
                .Include(d => d.Usuario)
                .Include(d => d.Especialidad)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.id == doctorId && d.especialidad_id == especialidadId && d.Usuario != null && d.Usuario.activo);

            if (doctor is null)
            {
                TempData["ErrorMessage"] = "No se encontró al doctor seleccionado.";
                return RedirectToAction(nameof(AgendarPaso2), new { especialidadId, citaId });
            }

            var fechaHoraSolicitud = fecha.ToDateTime(TimeOnly.FromTimeSpan(hora));
            var fechaMinimaReserva = DateTime.Today.AddDays(1);
            var esIntervaloValido = fechaHoraSolicitud >= fechaMinimaReserva;
            var disponible = esIntervaloValido && await EstaSlotDisponibleAsync(doctor.id, fecha, hora, citaId);

            var viewModel = new ScheduleConfirmationViewModel
            {
                EspecialidadId = especialidadId,
                DoctorId = doctorId,
                EspecialidadNombre = doctor.Especialidad?.nombre ?? string.Empty,
                DoctorEspecialidad = doctor.Especialidad?.nombre ?? string.Empty,
                DoctorNombre = ConstruirNombreCompleto(doctor),
                DoctorFoto = doctor.Usuario?.foto_url,
                PacienteNombre = string.Join(" ", new[] { paciente.nombre, paciente.apellido, paciente.segundo_apellido }.Where(s => !string.IsNullOrWhiteSpace(s))),
                PacienteCorreo = paciente.Usuario?.correo,
                PacienteTelefono = paciente.Usuario?.celular,
                Fecha = fecha,
                Hora = hora,
                EsVirtual = false,
                Ubicacion = string.IsNullOrWhiteSpace(doctor.consultorio) ? "Consultorio por confirmar" : doctor.consultorio,
                PuedeConfirmar = disponible,
                MensajeEstado = disponible ? (citaId.HasValue ? "Estás reprogramando una cita existente. Revisa la información antes de confirmar." : null) : (esIntervaloValido ? "El horario seleccionado ya no está disponible. Elige otro horario por favor." : "Las citas deben agendarse con al menos un día de anticipación."),
                CitaId = citaId
            };

            if (!disponible)
            {
                TempData["ErrorMessage"] = viewModel.MensajeEstado;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Paciente)]
        public async Task<IActionResult> ConfirmarCita(ScheduleConfirmationInput input)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(AgendarPaso3), new
                {
                    especialidadId = input.EspecialidadId,
                    doctorId = input.DoctorId,
                    fecha = input.Fecha,
                    hora = input.Hora,
                    citaId = input.CitaId
                });
            }

            var userId = User.GetUserId();
            if (userId is null)
            {
                return Forbid();
            }

            var paciente = await _perfilService.ObtenerPacienteAsync(userId.Value);
            if (paciente is null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctores
                .Include(d => d.Usuario)
                .FirstOrDefaultAsync(d => d.id == input.DoctorId && d.especialidad_id == input.EspecialidadId && d.Usuario != null && d.Usuario.activo);

            if (doctor is null)
            {
                TempData["ErrorMessage"] = "No se encontró al doctor seleccionado.";
                return RedirectToAction(nameof(AgendarPaso2), new { especialidadId = input.EspecialidadId, citaId = input.CitaId });
            }

            Cita? citaExistente = null;
            if (input.CitaId.HasValue)
            {
                citaExistente = await _context.Citas
                    .FirstOrDefaultAsync(c => c.id == input.CitaId.Value && c.paciente_id == paciente.id);

                if (citaExistente is null)
                {
                    TempData["ErrorMessage"] = "No encontramos la cita que deseas reprogramar.";
                    return RedirectToAction(nameof(MisCitas));
                }
            }

            var fechaSeleccionada = input.Fecha.ToDateTime(TimeOnly.MinValue);
            var motivoNormalizado = string.IsNullOrWhiteSpace(input.Motivo) ? null : input.Motivo.Trim();
            var notasNormalizadas = string.IsNullOrWhiteSpace(input.Notas) ? null : input.Notas.Trim();

            bool esMismoHorario = citaExistente is not null &&
                                   citaExistente.doctor_id == doctor.id &&
                                   DateOnly.FromDateTime(citaExistente.fecha.Date) == input.Fecha &&
                                   citaExistente.hora == input.Hora;

            var fechaHoraSeleccionada = input.Fecha.ToDateTime(TimeOnly.FromTimeSpan(input.Hora));

            if (!esMismoHorario && fechaHoraSeleccionada < DateTime.Today.AddDays(1))
            {
                TempData["ErrorMessage"] = "Las citas deben agendarse con al menos un día de anticipación.";
                return RedirectToAction(nameof(AgendarPaso2), new { especialidadId = input.EspecialidadId, doctorId = input.DoctorId, citaId = input.CitaId });
            }

            if (!esMismoHorario && !await EstaSlotDisponibleAsync(doctor.id, input.Fecha, input.Hora, citaExistente?.id))
            {
                TempData["ErrorMessage"] = "El horario seleccionado ya no está disponible. Elige otro horario.";
                return RedirectToAction(nameof(AgendarPaso2), new { especialidadId = input.EspecialidadId, doctorId = input.DoctorId, citaId = input.CitaId });
            }

            if (citaExistente is not null)
            {
                var fechaAnterior = citaExistente.fecha.Date.Add(citaExistente.hora);

                citaExistente.doctor_id = doctor.id;
                citaExistente.fecha = fechaSeleccionada;
                citaExistente.hora = input.Hora;
                citaExistente.motivo = motivoNormalizado;
                citaExistente.notas = notasNormalizadas;
                citaExistente.es_virtual = input.EsVirtual;
                citaExistente.estado = "PENDIENTE";
                citaExistente.actualizada_el = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var fechaNueva = citaExistente.fecha.Date.Add(citaExistente.hora);
                await _notificationService.NotificarCitaReprogramadaAsync(
                    citaExistente.doctor_id,
                    citaExistente.paciente_id,
                    fechaAnterior,
                    fechaNueva,
                    "Reprogramada por el paciente");

                TempData["SuccessMessage"] = "Tu cita se reprogramó correctamente. Recibirás una confirmación.";
                return RedirectToAction(nameof(MisCitas));
            }

            var cita = new Cita
            {
                paciente_id = paciente.id,
                doctor_id = doctor.id,
                fecha = fechaSeleccionada,
                hora = input.Hora,
                motivo = motivoNormalizado,
                notas = notasNormalizadas,
                es_virtual = input.EsVirtual,
                estado = "PENDIENTE",
                duracion_minutos = 30,
                creada_el = DateTime.UtcNow
            };

            _context.Citas.Add(cita);
            await _context.SaveChangesAsync();

            var fechaCita = cita.fecha.Date.Add(cita.hora);
            await _notificationService.NotificarCitaCreadaAsync(cita.doctor_id, cita.paciente_id, fechaCita, cita.motivo ?? "Consulta médica");

            TempData["SuccessMessage"] = "Tu cita se registró correctamente. Recibirás una notificación de confirmación.";
            return RedirectToAction(nameof(MisCitas));
        }

        private static string ConstruirNombreCompleto(Doctor doctor)
        {
            var partes = new[] { doctor.nombre, doctor.apellido, doctor.segundo_apellido }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim());

            return string.Join(" ", partes);
        }

        private static string? ConstruirResumenDoctor(Doctor doctor)
        {
            if (!string.IsNullOrWhiteSpace(doctor.biografia))
            {
                return doctor.biografia.Length > 120
                    ? doctor.biografia[..120] + "…"
                    : doctor.biografia;
            }

            return string.IsNullOrWhiteSpace(doctor.consultorio)
                ? null
                : $"Consultorio: {doctor.consultorio}";
        }

        [Authorize(Roles = Roles.Paciente)]
        [HttpGet]
        public async Task<IActionResult> GetDisponibilidadDoctor(long doctorId, long? citaId = null)
        {
            var doctor = await _context.Doctores
                .Include(d => d.Usuario)
                .FirstOrDefaultAsync(d => d.id == doctorId && d.Usuario != null && d.Usuario.activo);

            if (doctor is null)
            {
                return Json(Array.Empty<object>());
            }

            var (agenda, _, _) = await ConstruirAgendaDoctorAsync(doctorId, null, null, citaId);

            const int defaultSlotMinutes = 30;
            var eventos = agenda
                .SelectMany(dia => dia.Horas
                    .Where(h => h.Disponible)
                    .Select(h =>
                    {
                        var inicio = dia.Fecha.ToDateTime(TimeOnly.FromTimeSpan(h.Hora));
                        var fin = inicio.AddMinutes(defaultSlotMinutes);
                        return new
                        {
                            title = inicio.ToString("HH:mm"),
                            start = inicio.ToString("yyyy-MM-ddTHH:mm:ss"),
                            end = fin.ToString("yyyy-MM-ddTHH:mm:ss"),
                            display = "block",
                            extendedProps = new
                            {
                                fecha = dia.Fecha.ToString("yyyy-MM-dd"),
                                hora = h.Hora.ToString(@"hh\:mm\:ss")
                            }
                        };
                    }))
                .ToList();

            return Json(eventos);
        }

        private async Task<(List<ScheduleDayAvailability> Agenda, DateOnly? FechaSugerida, TimeSpan? HoraSugerida)> ConstruirAgendaDoctorAsync(long doctorId, DateOnly? fechaSeleccionada, TimeSpan? horaSeleccionada, long? citaIdIgnorar = null)
        {
            var cultura = CultureInfo.GetCultureInfo("es-MX");
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var fechaInicio = hoy;
            var fechaFin = hoy.AddDays(13);

            var inicio = fechaInicio.ToDateTime(TimeOnly.MinValue);
            var fin = fechaFin.ToDateTime(new TimeOnly(23, 59, 59));

            var citas = await _context.Citas
                .AsNoTracking()
                .Where(c => c.doctor_id == doctorId && c.fecha >= inicio && c.fecha <= fin)
                .Where(c => !citaIdIgnorar.HasValue || c.id != citaIdIgnorar.Value)
                .Select(c => new
                {
                    Fecha = DateOnly.FromDateTime(c.fecha.Date),
                    c.hora,
                    c.estado
                })
                .ToListAsync();

            var bloqueadas = citas
                .Where(c => EstadosBloqueantes.Contains((c.estado ?? string.Empty).Trim()))
                .GroupBy(c => c.Fecha)
                .ToDictionary(g => g.Key, g => g.Select(x => x.hora).ToHashSet());

            var agenda = new List<ScheduleDayAvailability>();
            DateOnly? primeraFechaDisponible = null;
            TimeSpan? primeraHoraDisponible = null;
            var fechaMinima = DateTime.Today.AddDays(1);

            foreach (var offset in Enumerable.Range(0, 14))
            {
                var dia = hoy.AddDays(offset);
                var esHoy = dia == hoy;
                var horas = new List<ScheduleHourAvailability>();
                var ocupado = bloqueadas.TryGetValue(dia, out var horasOcupadas) ? horasOcupadas : new HashSet<TimeSpan>();
                var hayDisponibilidad = false;

                foreach (var slot in HorariosLaborales)
                {
                    var slotDateTime = dia.ToDateTime(TimeOnly.FromTimeSpan(slot));
                    var esFuturo = slotDateTime >= fechaMinima;
                    var libre = esFuturo && !ocupado.Contains(slot);

                    if (libre && primeraFechaDisponible is null)
                    {
                        primeraFechaDisponible = dia;
                        primeraHoraDisponible = slot;
                    }

                    var seleccionado = fechaSeleccionada.HasValue && horaSeleccionada.HasValue && fechaSeleccionada.Value == dia && horaSeleccionada.Value == slot;

                    horas.Add(new ScheduleHourAvailability(slot, libre, seleccionado));
                    hayDisponibilidad |= libre;
                }

                var etiqueta = cultura.TextInfo.ToTitleCase(dia.ToDateTime(TimeOnly.MinValue).ToString("ddd d MMM", cultura));
                agenda.Add(new ScheduleDayAvailability(dia, etiqueta, esHoy, hayDisponibilidad, horas));
            }

            return (agenda, primeraFechaDisponible, primeraHoraDisponible);
        }

        private async Task<bool> EstaSlotDisponibleAsync(long doctorId, DateOnly fecha, TimeSpan hora, long? citaIdIgnorar = null)
        {
            var fechaHora = fecha.ToDateTime(TimeOnly.FromTimeSpan(hora));

            if (fechaHora < DateTime.Today.AddDays(1))
            {
                return false;
            }

            var fechaInicio = fecha.ToDateTime(TimeOnly.MinValue);
            var fechaFin = fecha.ToDateTime(TimeOnly.MaxValue);

            var existe = await _context.Citas
                .AsNoTracking()
                .Where(c => c.doctor_id == doctorId && c.fecha >= fechaInicio && c.fecha <= fechaFin && c.hora == hora)
                .Where(c => !citaIdIgnorar.HasValue || c.id != citaIdIgnorar.Value)
                .AnyAsync(c => EstadosBloqueantes.Contains((c.estado ?? string.Empty).Trim()));

            return !existe;
        }

        // AJAX Endpoints for modal functionality
        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] CreateCitaAjaxModel model)
        {
            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Datos inválidos" });
                }

                // Get current doctor
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentDoctor = await _context.Doctores
                    .FirstOrDefaultAsync(d => d.Usuario.usuario == User.Identity.Name);

                if (currentDoctor == null)
                {
                    return Json(new { success = false, message = "Doctor no encontrado" });
                }

                // Create new appointment
                var cita = new Cita
                {
                    paciente_id = model.paciente_id,
                    doctor_id = currentDoctor.id,
                    fecha = model.fecha,
                    hora = TimeSpan.Parse(model.hora),
                    motivo = model.motivo,
                    notas = model.notas,
                    es_virtual = model.es_virtual,
                    duracion_minutos = model.duracion_minutos,
                    estado = "PENDIENTE"
                };

                // Validate business rules
                if (!cita.EsFechaValida())
                {
                    return Json(new { success = false, message = "La fecha y hora deben ser futuras" });
                }

                if (!cita.EsHorarioLaboral())
                {
                    return Json(new { success = false, message = "La cita debe estar en horario laboral (8:00-17:00, lunes a sábado)" });
                }

                // Check for conflicts
                var hasConflict = await _context.Citas
                    .AnyAsync(c => c.doctor_id == currentDoctor.id &&
                                  c.fecha == cita.fecha &&
                                  c.estado != "CANCELADA" &&
                                  ((c.hora <= cita.hora && cita.hora < c.hora.Add(TimeSpan.FromMinutes(c.duracion_minutos))) ||
                                   (cita.hora <= c.hora && c.hora < cita.hora.Add(TimeSpan.FromMinutes(cita.duracion_minutos)))));

                if (hasConflict)
                {
                    return Json(new { success = false, message = "Ya existe una cita en ese horario" });
                }

                _context.Citas.Add(cita);
                await _context.SaveChangesAsync();

                // Send notification
                await _notificationService.NotificarCitaCreadaAsync(
                    cita.doctor_id, 
                    cita.paciente_id, 
                    cita.fecha.Add(cita.hora), 
                    cita.motivo ?? "Sin motivo especificado"
                );

                return Json(new { success = true, message = "Cita creada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment via AJAX");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDetailsAjax(long id)
        {
            try
            {
                var cita = await _context.Citas
                    .Include(c => c.Paciente)
                    .Include(c => c.Doctor)
                    .ThenInclude(d => d.Especialidad)
                    .FirstOrDefaultAsync(c => c.id == id);

                if (cita == null)
                {
                    return Json(new { success = false, message = "Cita no encontrada" });
                }

                var ubicacionDescripcion = cita.es_virtual
                    ? "Consulta virtual"
                    : (string.IsNullOrWhiteSpace(cita.Doctor?.consultorio) ? "Consultorio por confirmar" : cita.Doctor.consultorio);

                var result = new
                {
                    success = true,
                    cita = new
                    {
                        id = cita.id,
                        fecha = cita.fecha.ToString("yyyy-MM-dd"),
                        hora = cita.hora.ToString(@"hh\:mm"),
                        motivo = cita.motivo,
                        notas = cita.notas,
                        ubicacion = ubicacionDescripcion,
                        es_virtual = cita.es_virtual,
                        duracion_minutos = cita.duracion_minutos,
                        estado = cita.estado,
                        motivo_cancelacion = cita.motivo_cancelacion,
                        paciente = new
                        {
                            id = cita.Paciente.id,
                            nombre = cita.Paciente.nombre,
                            apellido = cita.Paciente.apellido,
                            fecha_nacimiento = cita.Paciente.fecha_nacimiento.ToString("yyyy-MM-dd"),
                            genero = cita.Paciente.genero
                        }
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointment details via AJAX");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatusAjax(long id, [FromBody] UpdateStatusModel model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.estado))
                {
                    return Json(new { success = false, message = "Estado inválido" });
                }

                var nuevoEstado = model.estado.Trim().ToUpperInvariant();
                var estadosPermitidos = new[] { "PENDIENTE", "CONFIRMADA", "COMPLETADA", "CANCELADA", "INICIADA" };
                if (!estadosPermitidos.Contains(nuevoEstado))
                {
                    return Json(new { success = false, message = "Estado no permitido" });
                }

                var cita = await _context.Citas
                    .Include(c => c.Paciente)
                    .Include(c => c.Doctor)
                    .FirstOrDefaultAsync(c => c.id == id);

                if (cita == null)
                {
                    return Json(new { success = false, message = "Cita no encontrada" });
                }

                var role = User.GetRole();
                var userId = User.GetUserId();

                if (role == Roles.Doctor)
                {
                    if (userId is null)
                    {
                        return Json(new { success = false, message = "Usuario no autenticado" });
                    }

                    var doctor = await _perfilService.ObtenerDoctorAsync(userId.Value);
                    if (doctor == null || doctor.id != cita.doctor_id)
                    {
                        return Json(new { success = false, message = "No tienes permisos para modificar esta cita" });
                    }
                }
                else if (role != Roles.Administrador)
                {
                    return Json(new { success = false, message = "No tienes permisos para modificar esta cita" });
                }

                if (string.Equals(cita.estado, nuevoEstado, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = true, message = "La cita ya se encuentra en este estado" });
                }

                if (nuevoEstado == "INICIADA")
                {
                    if (string.Equals(cita.estado, "COMPLETADA", StringComparison.OrdinalIgnoreCase) || string.Equals(cita.estado, "CANCELADA", StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new { success = false, message = "La cita ya no puede iniciarse." });
                    }
                }

                if (nuevoEstado == "CANCELADA" && string.IsNullOrWhiteSpace(model.motivo_cancelacion))
                {
                    return Json(new { success = false, message = "Debes indicar un motivo para cancelar" });
                }

                var estadoAnterior = cita.estado;
                cita.estado = nuevoEstado;
                cita.actualizada_el = DateTime.UtcNow;
                cita.motivo_cancelacion = nuevoEstado == "CANCELADA"
                    ? model.motivo_cancelacion?.Trim()
                    : null;

                await _context.SaveChangesAsync();

                var fechaCita = cita.fecha.Add(cita.hora);

                if (nuevoEstado == "COMPLETADA" && !string.Equals(estadoAnterior, "COMPLETADA", StringComparison.OrdinalIgnoreCase))
                {
                    await _notificationService.NotificarCitaCompletadaAsync(cita.doctor_id, cita.paciente_id, fechaCita);
                }
                else if (nuevoEstado == "CANCELADA" && !string.Equals(estadoAnterior, "CANCELADA", StringComparison.OrdinalIgnoreCase))
                {
                    var motivo = string.IsNullOrWhiteSpace(cita.motivo_cancelacion)
                        ? "Cancelada por el doctor"
                        : cita.motivo_cancelacion;

                    await _notificationService.NotificarCitaCanceladaAsync(cita.doctor_id, cita.paciente_id, fechaCita, motivo!);
                }

                return Json(new { success = true, message = "Estado actualizado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment status via AJAX");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarConsulta(long id, [FromBody] FinalizarConsultaModel model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.diagnostico) || string.IsNullOrWhiteSpace(model.tratamiento))
                {
                    return Json(new { success = false, message = "Ingresa diagnóstico y tratamiento recomendado." });
                }

                var cita = await _context.Citas
                    .Include(c => c.Paciente)
                    .Include(c => c.Doctor)
                    .FirstOrDefaultAsync(c => c.id == id);

                if (cita == null)
                {
                    return Json(new { success = false, message = "Cita no encontrada" });
                }

                var role = User.GetRole();
                var userId = User.GetUserId();

                if (role == Roles.Doctor)
                {
                    if (userId is null)
                    {
                        return Json(new { success = false, message = "Usuario no autenticado" });
                    }

                    var doctor = await _perfilService.ObtenerDoctorAsync(userId.Value);
                    if (doctor == null || doctor.id != cita.doctor_id)
                    {
                        return Json(new { success = false, message = "No tienes permisos para finalizar esta consulta" });
                    }
                }
                else if (role != Roles.Administrador)
                {
                    return Json(new { success = false, message = "No tienes permisos para finalizar esta consulta" });
                }

                if (string.Equals(cita.estado, "COMPLETADA", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "La cita ya fue finalizada." });
                }

                if (!string.Equals(cita.estado, "INICIADA", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Debes iniciar la consulta antes de finalizarla." });
                }

                DateOnly? seguimientoFecha = null;
                TimeSpan? seguimientoHora = null;

                if (model.agendarSeguimiento)
                {
                    if (string.IsNullOrWhiteSpace(model.seguimiento_fecha) || string.IsNullOrWhiteSpace(model.seguimiento_hora))
                    {
                        return Json(new { success = false, message = "Completa la fecha y hora del seguimiento." });
                    }

                    if (!DateOnly.TryParse(model.seguimiento_fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaParseada))
                    {
                        return Json(new { success = false, message = "Fecha de seguimiento inválida." });
                    }

                    if (!TimeSpan.TryParse(model.seguimiento_hora, CultureInfo.InvariantCulture, out var horaParseada))
                    {
                        return Json(new { success = false, message = "Hora de seguimiento inválida." });
                    }

                    var fechaHoraSeguimiento = fechaParseada.ToDateTime(TimeOnly.FromTimeSpan(horaParseada));
                    if (fechaHoraSeguimiento <= DateTime.Now)
                    {
                        return Json(new { success = false, message = "La cita de seguimiento debe ser en el futuro." });
                    }

                    var disponible = await EstaSlotDisponibleAsync(cita.doctor_id, fechaParseada, horaParseada);
                    if (!disponible)
                    {
                        return Json(new { success = false, message = "El horario seleccionado para el seguimiento no está disponible." });
                    }

                    seguimientoFecha = fechaParseada;
                    seguimientoHora = horaParseada;
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();

                cita.diagnostico = model.diagnostico.Trim();
                cita.tratamiento_recomendado = model.tratamiento.Trim();
                cita.notas = string.IsNullOrWhiteSpace(model.notas) ? null : model.notas.Trim();
                cita.estado = "COMPLETADA";
                cita.actualizada_el = DateTime.UtcNow;

                Cita? citaSeguimiento = null;

                if (model.agendarSeguimiento && seguimientoFecha.HasValue && seguimientoHora.HasValue)
                {
                    citaSeguimiento = new Cita
                    {
                        paciente_id = cita.paciente_id,
                        doctor_id = cita.doctor_id,
                        fecha = seguimientoFecha.Value.ToDateTime(TimeOnly.MinValue),
                        hora = seguimientoHora.Value,
                        motivo = string.IsNullOrWhiteSpace(model.seguimiento_motivo) ? "Cita de seguimiento" : model.seguimiento_motivo.Trim(),
                        notas = null,
                        es_virtual = cita.es_virtual,
                        duracion_minutos = cita.duracion_minutos,
                        estado = "PENDIENTE",
                        creada_el = DateTime.UtcNow
                    };

                    _context.Citas.Add(citaSeguimiento);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var fechaOriginal = cita.fecha.Add(cita.hora);
                await _notificationService.NotificarCitaCompletadaAsync(cita.doctor_id, cita.paciente_id, fechaOriginal);

                if (citaSeguimiento != null)
                {
                    var fechaSeguimiento = citaSeguimiento.fecha.Add(citaSeguimiento.hora);
                    await _notificationService.NotificarCitaCreadaAsync(citaSeguimiento.doctor_id, citaSeguimiento.paciente_id, fechaSeguimiento, citaSeguimiento.motivo ?? "Cita de seguimiento");
                }

                return Json(new
                {
                    success = true,
                    message = citaSeguimiento == null
                        ? "Consulta finalizada correctamente."
                        : "Consulta finalizada y seguimiento agendado.",
                    seguimientoId = citaSeguimiento?.id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al finalizar la consulta {CitaId}", id);
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAjax(long id)
        {
            try
            {
                var cita = await _context.Citas
                    .Include(c => c.Paciente)
                    .FirstOrDefaultAsync(c => c.id == id);

                if (cita == null)
                {
                    return Json(new { success = false, message = "Cita no encontrada" });
                }

                // Send cancellation notification
                await _notificationService.NotificarCitaCanceladaAsync(
                    cita.doctor_id,
                    cita.paciente_id,
                    cita.fecha.Add(cita.hora),
                    "Cita eliminada por el doctor"
                );

                _context.Citas.Remove(cita);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cita eliminada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting appointment via AJAX");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPacientesAjax()
        {
            try
            {
                var pacientes = await _context.Pacientes
                    .Where(p => p.Usuario != null && p.Usuario.activo)
                    .Select(p => new
                    {
                        id = p.id,
                        nombre = p.nombre,
                        apellido = p.apellido
                    })
                    .ToListAsync();

                return Json(pacientes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patients via AJAX");
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }
    }

    // AJAX Models
    public class CreateCitaAjaxModel
    {
        public long paciente_id { get; set; }
        public DateTime fecha { get; set; }
        public string hora { get; set; } = "";
        public string? motivo { get; set; }
        public string? notas { get; set; }
        public bool es_virtual { get; set; }
        public int duracion_minutos { get; set; } = 30;
    }

    public class UpdateStatusModel
    {
        public string estado { get; set; } = "";
        public string? motivo_cancelacion { get; set; }
    }

    public class FinalizarConsultaModel
    {
        public string diagnostico { get; set; } = string.Empty;
        public string tratamiento { get; set; } = string.Empty;
        public string? notas { get; set; }
        public bool agendarSeguimiento { get; set; }
        public string? seguimiento_fecha { get; set; }
        public string? seguimiento_hora { get; set; }
        public string? seguimiento_motivo { get; set; }
    }
}
