using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CitaFacil.Services.Logging
{
    public class LoggerService : ILoggerService
    {
        private readonly ILogger<LoggerService> _logger;

        public LoggerService(ILogger<LoggerService> logger)
        {
            _logger = logger;
        }

        public void LogInfo(string message, object? data = null)
        {
            var logMessage = FormatMessage(message, data);
            _logger.LogInformation(logMessage);
        }

        public void LogWarning(string message, object? data = null)
        {
            var logMessage = FormatMessage(message, data);
            _logger.LogWarning(logMessage);
        }

        public void LogError(string message, Exception? exception = null, object? data = null)
        {
            var logMessage = FormatMessage(message, data);
            if (exception != null)
            {
                _logger.LogError(exception, logMessage);
            }
            else
            {
                _logger.LogError(logMessage);
            }
        }

        public void LogDebug(string message, object? data = null)
        {
            var logMessage = FormatMessage(message, data);
            _logger.LogDebug(logMessage);
        }

        public void LogUserAction(string userId, string action, string details = "")
        {
            var logData = new
            {
                UserId = userId,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
            
            var message = $"Usuario {userId} ejecutó acción: {action}";
            LogInfo(message, logData);
        }

        public void LogBusinessEvent(string eventType, string details, object? data = null)
        {
            var logData = new
            {
                EventType = eventType,
                Details = details,
                Data = data,
                Timestamp = DateTime.UtcNow
            };
            
            var message = $"Evento de negocio: {eventType} - {details}";
            LogInfo(message, logData);
        }

        private string FormatMessage(string message, object? data)
        {
            if (data == null)
                return message;

            try
            {
                var dataJson = JsonSerializer.Serialize(data, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });
                return $"{message} | Data: {dataJson}";
            }
            catch
            {
                return $"{message} | Data: {data}";
            }
        }
    }
}