namespace CitaFacil.Services.DatabaseBackup;

public record DatabaseBackupResult(bool Success, string Message, string? FilePath = null, string? FileName = null);

