using CitaFacil.Models;

namespace CitaFacil.Services.DatabaseBackup;

public interface IDatabaseBackupService
{
    Task<DatabaseBackupConfig> GetOrCreateConfigurationAsync(CancellationToken cancellationToken = default);

    Task UpdateConfigurationAsync(DatabaseBackupConfig config, CancellationToken cancellationToken = default);

    Task<DatabaseBackupResult> CreateBackupAsync(bool automatic, CancellationToken cancellationToken = default);

    Task<DatabaseBackupResult> RestoreAsync(string filePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatabaseBackupHistory>> GetHistoryAsync(int take = 20, CancellationToken cancellationToken = default);

    Task CleanupOldBackupsAsync(CancellationToken cancellationToken = default);
}

