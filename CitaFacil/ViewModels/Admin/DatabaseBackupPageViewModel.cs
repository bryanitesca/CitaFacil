using CitaFacil.Models;

namespace CitaFacil.ViewModels.Admin;

public class DatabaseBackupPageViewModel
{
    public DatabaseBackupConfigViewModel Config { get; set; } = new();

    public IReadOnlyList<DatabaseBackupHistory> History { get; set; } = Array.Empty<DatabaseBackupHistory>();

    public string? LastError { get; set; }
}

