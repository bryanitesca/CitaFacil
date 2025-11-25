using System;

namespace CitaFacil.Services.Logging
{
    public interface ILoggerService
    {
        void LogInfo(string message, object? data = null);
        void LogWarning(string message, object? data = null);
        void LogError(string message, Exception? exception = null, object? data = null);
        void LogDebug(string message, object? data = null);
        void LogUserAction(string userId, string action, string details = "");
        void LogBusinessEvent(string eventType, string details, object? data = null);
    }
}