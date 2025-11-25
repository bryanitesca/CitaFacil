using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitaFacil.Data;
using CitaFacil.Models;
using CitaFacil.Services.Notifications.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CitaFacil.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<NotificationResult> RegistrarContactoPacienteAsync(long doctorId, long pacienteId, NotificationRequest request)
        {
            var notificacion = new Notificacion
            {
                doctor_id = doctorId,
                paciente_id = pacienteId,
                asunto = request.Asunto.Trim(),
                mensaje = request.Mensaje.Trim(),
                via_sistema = true,
                leida = false,
                enviada_el = DateTime.UtcNow
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Notificación registrada: Doctor={DoctorId}, Paciente={PacienteId}, ViaSistema=true, Asunto={Asunto}",
                doctorId,
                pacienteId,
                request.Asunto);

            return new NotificationResult(true, "El contacto se registró correctamente.");
        }

        public async Task<IReadOnlyList<NotificationRecord>> ObtenerHistorialAsync(long doctorId, long pacienteId)
        {
            return await _context.Notificaciones
                .Where(n => n.doctor_id == doctorId && n.paciente_id == pacienteId)
                .OrderByDescending(n => n.enviada_el)
                .Select(n => new NotificationRecord(n.enviada_el, n.asunto, n.mensaje, n.via_sistema, n.leida))
                .ToListAsync();
        }

        // New notification methods for key system actions
        public async Task<NotificationResult> NotificarCitaCreadaAsync(long doctorId, long pacienteId, DateTime fechaCita, string motivo)
        {
            var notificacion = new Notificacion
            {
                doctor_id = doctorId,
                paciente_id = pacienteId,
                asunto = "Nueva cita programada",
                mensaje = $"Se ha programado una nueva cita para el {fechaCita:dd/MM/yyyy HH:mm}. Motivo: {motivo}",
                via_sistema = true,
                leida = false,
                enviada_el = DateTime.UtcNow
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notificación de cita creada enviada: Doctor={DoctorId}, Paciente={PacienteId}, Fecha={Fecha}", 
                doctorId, pacienteId, fechaCita);

            return new NotificationResult(true, "Notificación de cita creada enviada correctamente.");
        }

        public async Task<NotificationResult> NotificarCitaCanceladaAsync(long doctorId, long pacienteId, DateTime fechaCita, string motivo)
        {
            var notificacion = new Notificacion
            {
                doctor_id = doctorId,
                paciente_id = pacienteId,
                asunto = "Cita cancelada",
                mensaje = $"Su cita del {fechaCita:dd/MM/yyyy HH:mm} ha sido cancelada. Motivo: {motivo}",
                via_sistema = true,
                leida = false,
                enviada_el = DateTime.UtcNow
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notificación de cita cancelada enviada: Doctor={DoctorId}, Paciente={PacienteId}, Fecha={Fecha}", 
                doctorId, pacienteId, fechaCita);

            return new NotificationResult(true, "Notificación de cita cancelada enviada correctamente.");
        }

        public async Task<NotificationResult> NotificarCitaReprogramadaAsync(long doctorId, long pacienteId, DateTime fechaAnterior, DateTime fechaNueva, string motivo)
        {
            var notificacion = new Notificacion
            {
                doctor_id = doctorId,
                paciente_id = pacienteId,
                asunto = "Cita reprogramada",
                mensaje = $"Su cita del {fechaAnterior:dd/MM/yyyy HH:mm} ha sido reprogramada para el {fechaNueva:dd/MM/yyyy HH:mm}. {(!string.IsNullOrEmpty(motivo) ? $"Motivo: {motivo}" : "")}",
                via_sistema = true,
                leida = false,
                enviada_el = DateTime.UtcNow
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notificación de cita reprogramada enviada: Doctor={DoctorId}, Paciente={PacienteId}, FechaAnterior={FechaAnterior}, FechaNueva={FechaNueva}", 
                doctorId, pacienteId, fechaAnterior, fechaNueva);

            return new NotificationResult(true, "Notificación de cita reprogramada enviada correctamente.");
        }

        public async Task<NotificationResult> NotificarCitaCompletadaAsync(long doctorId, long pacienteId, DateTime fechaCita)
        {
            var notificacion = new Notificacion
            {
                doctor_id = doctorId,
                paciente_id = pacienteId,
                asunto = "Cita completada",
                mensaje = $"Su cita del {fechaCita:dd/MM/yyyy HH:mm} ha sido completada. Gracias por su visita.",
                via_sistema = true,
                leida = false,
                enviada_el = DateTime.UtcNow
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notificación de cita completada enviada: Doctor={DoctorId}, Paciente={PacienteId}, Fecha={Fecha}", 
                doctorId, pacienteId, fechaCita);

            return new NotificationResult(true, "Notificación de cita completada enviada correctamente.");
        }

        public async Task<NotificationResult> NotificarPromovcionDoctorAsync(long usuarioId, long especialidadId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            var especialidad = await _context.Especialidades.FindAsync(especialidadId);

            if (usuario == null || especialidad == null)
                return new NotificationResult(false, "Usuario o especialidad no encontrados.");

            _logger.LogInformation("Usuario {UsuarioId} promovido a doctor en especialidad {EspecialidadId}", 
                usuarioId, especialidadId);

            return new NotificationResult(true, $"Usuario {usuario.nombre} promovido a doctor en {especialidad.nombre}.");
        }

        // Implementación de métodos para notificaciones del Doctor
        public async Task<IReadOnlyList<NotificationDto>> ObtenerNotificacionesDoctorAsync(long doctorId, bool soloNoLeidas = false)
        {
            var query = _context.Notificaciones
                .Where(n => n.doctor_id == doctorId);

            if (soloNoLeidas)
            {
                query = query.Where(n => !n.leida);
            }

            var notificaciones = await query
                .OrderByDescending(n => n.enviada_el)
                .Take(50) // Limitar a las últimas 50 notificaciones
                .ToListAsync();

            return notificaciones.Select(n => new NotificationDto(
                n.id,
                n.asunto,
                n.mensaje,
                n.enviada_el,
                n.leida,
                CalcularTiempoTranscurrido(n.enviada_el)
            )).ToList();
        }

        public async Task<int> ContarNotificacionesNoLeidasDoctorAsync(long doctorId)
        {
            return await _context.Notificaciones
                .Where(n => n.doctor_id == doctorId && !n.leida)
                .CountAsync();
        }

        public async Task<NotificationResult> MarcarNotificacionLeidaAsync(long notificacionId, long doctorId)
        {
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.id == notificacionId && n.doctor_id == doctorId);

            if (notificacion == null)
            {
                return new NotificationResult(false, "Notificación no encontrada.");
            }

            notificacion.leida = true;
            await _context.SaveChangesAsync();

            return new NotificationResult(true, "Notificación marcada como leída.");
        }

        public async Task<NotificationResult> MarcarTodasLeidasDoctorAsync(long doctorId)
        {
            var notificaciones = await _context.Notificaciones
                .Where(n => n.doctor_id == doctorId && !n.leida)
                .ToListAsync();

            foreach (var notificacion in notificaciones)
            {
                notificacion.leida = true;
            }

            await _context.SaveChangesAsync();

            return new NotificationResult(true, $"{notificaciones.Count} notificaciones marcadas como leídas.");
        }

        // Implementación de métodos para notificaciones del Paciente
        public async Task<IReadOnlyList<NotificationDto>> ObtenerNotificacionesPacienteAsync(long pacienteId, bool soloNoLeidas = false)
        {
            var query = _context.Notificaciones
                .Where(n => n.paciente_id == pacienteId);

            if (soloNoLeidas)
            {
                query = query.Where(n => !n.leida);
            }

            var notificaciones = await query
                .OrderByDescending(n => n.enviada_el)
                .Take(50)
                .ToListAsync();

            return notificaciones.Select(n => new NotificationDto(
                n.id,
                n.asunto,
                n.mensaje,
                n.enviada_el,
                n.leida,
                CalcularTiempoTranscurrido(n.enviada_el)
            )).ToList();
        }

        public async Task<int> ContarNotificacionesNoLeidasPacienteAsync(long pacienteId)
        {
            return await _context.Notificaciones
                .Where(n => n.paciente_id == pacienteId && !n.leida)
                .CountAsync();
        }

        public async Task<NotificationResult> MarcarNotificacionLeidaPacienteAsync(long notificacionId, long pacienteId)
        {
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.id == notificacionId && n.paciente_id == pacienteId);

            if (notificacion == null)
            {
                return new NotificationResult(false, "Notificación no encontrada.");
            }

            notificacion.leida = true;
            await _context.SaveChangesAsync();

            return new NotificationResult(true, "Notificación marcada como leída.");
        }

        public async Task<NotificationResult> MarcarTodasLeidasPacienteAsync(long pacienteId)
        {
            var notificaciones = await _context.Notificaciones
                .Where(n => n.paciente_id == pacienteId && !n.leida)
                .ToListAsync();

            foreach (var notificacion in notificaciones)
            {
                notificacion.leida = true;
            }

            await _context.SaveChangesAsync();

            return new NotificationResult(true, $"{notificaciones.Count} notificaciones marcadas como leídas.");
        }

        // Método para crear notificaciones generales
        public async Task<NotificationResult> CrearNotificacionAsync(long? doctorId, long? pacienteId, string asunto, string mensaje)
        {
            if (!doctorId.HasValue && !pacienteId.HasValue)
            {
                return new NotificationResult(false, "Debe especificar al menos un doctor o paciente.");
            }

            var notificacion = new Notificacion
            {
                doctor_id = doctorId ?? 0,
                paciente_id = pacienteId ?? 0,
                asunto = asunto.Trim(),
                mensaje = mensaje.Trim(),
                via_sistema = true,
                leida = false,
                enviada_el = DateTime.UtcNow
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notificación creada: Doctor={DoctorId}, Paciente={PacienteId}, Asunto={Asunto}",
                doctorId, pacienteId, asunto);

            return new NotificationResult(true, "Notificación creada correctamente.");
        }

        private string CalcularTiempoTranscurrido(DateTime fecha)
        {
            var ahora = DateTime.UtcNow;
            var diferencia = ahora - fecha;

            if (diferencia.TotalMinutes < 1)
                return "Hace un momento";
            if (diferencia.TotalMinutes < 60)
                return $"Hace {(int)diferencia.TotalMinutes} minutos";
            if (diferencia.TotalHours < 24)
                return $"Hace {(int)diferencia.TotalHours} horas";
            if (diferencia.TotalDays < 7)
                return $"Hace {(int)diferencia.TotalDays} días";
            if (diferencia.TotalDays < 30)
                return $"Hace {(int)(diferencia.TotalDays / 7)} semanas";
            if (diferencia.TotalDays < 365)
                return $"Hace {(int)(diferencia.TotalDays / 30)} meses";
            
            return $"Hace {(int)(diferencia.TotalDays / 365)} años";
        }
    }
}

