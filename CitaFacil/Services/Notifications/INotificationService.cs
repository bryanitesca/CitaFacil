using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitaFacil.Services.Notifications.Models;

namespace CitaFacil.Services.Notifications
{
    public interface INotificationService
    {
        Task<NotificationResult> RegistrarContactoPacienteAsync(long doctorId, long pacienteId, NotificationRequest request);

        Task<IReadOnlyList<NotificationRecord>> ObtenerHistorialAsync(long doctorId, long pacienteId);

        Task<NotificationResult> NotificarCitaCreadaAsync(long doctorId, long pacienteId, DateTime fechaCita, string motivo);

        Task<NotificationResult> NotificarCitaCanceladaAsync(long doctorId, long pacienteId, DateTime fechaCita, string motivo);

        Task<NotificationResult> NotificarCitaReprogramadaAsync(long doctorId, long pacienteId, DateTime fechaAnterior, DateTime fechaNueva, string motivo);

        Task<NotificationResult> NotificarCitaCompletadaAsync(long doctorId, long pacienteId, DateTime fechaCita);

        Task<NotificationResult> NotificarPromovcionDoctorAsync(long usuarioId, long especialidadId);

        // Nuevos métodos para notificaciones del Doctor
        Task<IReadOnlyList<NotificationDto>> ObtenerNotificacionesDoctorAsync(long doctorId, bool soloNoLeidas = false);
        
        Task<int> ContarNotificacionesNoLeidasDoctorAsync(long doctorId);
        
        Task<NotificationResult> MarcarNotificacionLeidaAsync(long notificacionId, long doctorId);
        
        Task<NotificationResult> MarcarTodasLeidasDoctorAsync(long doctorId);

        // Nuevos métodos para notificaciones del Paciente
        Task<IReadOnlyList<NotificationDto>> ObtenerNotificacionesPacienteAsync(long pacienteId, bool soloNoLeidas = false);
        
        Task<int> ContarNotificacionesNoLeidasPacienteAsync(long pacienteId);
        
        Task<NotificationResult> MarcarNotificacionLeidaPacienteAsync(long notificacionId, long pacienteId);
        
        Task<NotificationResult> MarcarTodasLeidasPacienteAsync(long pacienteId);

        // Método para crear notificaciones generales
        Task<NotificationResult> CrearNotificacionAsync(long? doctorId, long? pacienteId, string asunto, string mensaje);
    }
}

