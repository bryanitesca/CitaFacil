using System;

namespace CitaFacil.Services.Notifications.Models
{
    public record NotificationRequest(string Asunto, string Mensaje);

    public record NotificationResult(bool Exito, string Mensaje);

    public record NotificationRecord(DateTime Fecha, string Asunto, string Mensaje, bool ViaSistema, bool Leida);

    public record NotificationDto(
        long Id, 
        string Asunto, 
        string Mensaje, 
        DateTime Fecha, 
        bool Leida, 
        string TiempoTranscurrido
    );
}

