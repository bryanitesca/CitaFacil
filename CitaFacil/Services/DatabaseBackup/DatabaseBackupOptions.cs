namespace CitaFacil.Services.DatabaseBackup;

public class DatabaseBackupOptions
{
    public string DefaultDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "Backups");

    /// <summary>
    /// Hora local a la que se intentará ejecutar el respaldo automático cada día.
    /// </summary>
    public TimeSpan AutoBackupTime { get; set; } = new(2, 0, 0);

    public bool AutoBackupEnabled { get; set; } = true;

    public int RetentionDays { get; set; } = 30;
}

