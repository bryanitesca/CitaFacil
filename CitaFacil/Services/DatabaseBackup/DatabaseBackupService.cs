using CitaFacil.Data;
using CitaFacil.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CitaFacil.Services.DatabaseBackup;

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly ApplicationDbContext _context;
    private readonly DatabaseBackupOptions _options;
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly string _databaseName;
    private readonly string _masterConnectionString;
    private bool? _supportsCompression;

    public DatabaseBackupService(
        ApplicationDbContext context,
        IOptions<DatabaseBackupOptions> options,
        IConfiguration configuration,
        ILogger<DatabaseBackupService> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        _databaseName = builder.InitialCatalog;

        var masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master"
        };
        _masterConnectionString = masterBuilder.ConnectionString;
    }

    public async Task<DatabaseBackupConfig> GetOrCreateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var config = await _context.DatabaseBackupConfigs
            .OrderBy(c => c.id)
            .FirstOrDefaultAsync(cancellationToken);
        if (config != null)
        {
            return config;
        }

        config = new DatabaseBackupConfig
        {
            backup_directory = NormalizeDirectory(_options.DefaultDirectory),
            auto_backup_enabled = _options.AutoBackupEnabled,
            auto_backup_time = _options.AutoBackupTime,
            retention_days = Math.Clamp(_options.RetentionDays, 0, 30)
        };

        _context.DatabaseBackupConfigs.Add(config);
        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task UpdateConfigurationAsync(DatabaseBackupConfig config, CancellationToken cancellationToken = default)
    {
        var existing = await GetOrCreateConfigurationAsync(cancellationToken);
        existing.backup_directory = NormalizeDirectory(config.backup_directory);
        existing.auto_backup_enabled = config.auto_backup_enabled;
        existing.auto_backup_time = config.auto_backup_time;
        existing.retention_days = Math.Clamp(config.retention_days, 0, 30);
        EnsureDirectory(existing.backup_directory);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DatabaseBackupResult> CreateBackupAsync(bool automatic, CancellationToken cancellationToken = default)
    {
        var config = await GetOrCreateConfigurationAsync(cancellationToken);
        var directory = NormalizeDirectory(config.backup_directory);
        EnsureDirectory(directory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var operationTag = automatic ? "AUTO" : "MANUAL";
        var fileName = $"{_databaseName}_{timestamp}_{operationTag}.bak";
        var filePath = Path.Combine(directory, fileName);

        try
        {
            await ExecuteBackupCommandAsync(filePath, cancellationToken);

            var history = new DatabaseBackupHistory
            {
                operation_type = automatic ? "AUTOMATIC" : "MANUAL",
                status = "SUCCESS",
                file_path = filePath,
                file_name = fileName,
                created_utc = DateTime.UtcNow
            };

            _context.DatabaseBackupHistory.Add(history);

            config.last_backup_utc = DateTime.UtcNow;
            if (automatic)
            {
                config.last_automatic_backup_utc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await CleanupOldBackupsAsync(cancellationToken);

            return new DatabaseBackupResult(true, "Respaldo generado correctamente.", filePath, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar respaldo de base de datos en {Ruta}", filePath);

            var history = new DatabaseBackupHistory
            {
                operation_type = automatic ? "AUTOMATIC" : "MANUAL",
                status = "FAILED",
                file_path = filePath,
                file_name = fileName,
                created_utc = DateTime.UtcNow,
                message = ex.Message
            };

            _context.DatabaseBackupHistory.Add(history);
            await _context.SaveChangesAsync(cancellationToken);

            var message = ex.Message.Contains("COMPRESSION is not supported", StringComparison.OrdinalIgnoreCase)
                ? "El servidor SQL Server Express no soporta compresión. El respaldo se generará sin compresión si repites la operación."
                : ex.Message;

            return new DatabaseBackupResult(false, $"No se pudo completar el respaldo: {message}");
        }
    }

    public async Task<DatabaseBackupResult> RestoreAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new DatabaseBackupResult(false, "Debe proporcionar la ruta del archivo .bak.");
        }

        if (!Path.GetExtension(filePath).Equals(".bak", StringComparison.OrdinalIgnoreCase))
        {
            return new DatabaseBackupResult(false, "El archivo debe tener extensión .bak.");
        }

        if (!File.Exists(filePath))
        {
            return new DatabaseBackupResult(false, "El archivo de respaldo no existe en el servidor.");
        }

        var normalizedPath = Path.GetFullPath(filePath);
        var fileName = Path.GetFileName(normalizedPath);

        try
        {
            await ExecuteRestoreCommandAsync(normalizedPath, cancellationToken);

            var history = new DatabaseBackupHistory
            {
                operation_type = "RESTORE",
                status = "SUCCESS",
                file_path = normalizedPath,
                file_name = fileName,
                created_utc = DateTime.UtcNow
            };

            _context.DatabaseBackupHistory.Add(history);
            await _context.SaveChangesAsync(cancellationToken);

            return new DatabaseBackupResult(true, "Restauración completada correctamente.", normalizedPath, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al restaurar la base de datos desde {Ruta}", normalizedPath);

            var history = new DatabaseBackupHistory
            {
                operation_type = "RESTORE",
                status = "FAILED",
                file_path = normalizedPath,
                file_name = fileName,
                created_utc = DateTime.UtcNow,
                message = ex.Message
            };

            _context.DatabaseBackupHistory.Add(history);
            await _context.SaveChangesAsync(cancellationToken);

            return new DatabaseBackupResult(false, $"No se pudo restaurar la base de datos: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<DatabaseBackupHistory>> GetHistoryAsync(int take = 20, CancellationToken cancellationToken = default)
    {
        return await _context.DatabaseBackupHistory
            .OrderByDescending(h => h.created_utc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task CleanupOldBackupsAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetOrCreateConfigurationAsync(cancellationToken);
        if (config.retention_days <= 0)
        {
            return;
        }

        var threshold = DateTime.UtcNow.AddDays(-config.retention_days);
        var oldEntries = await _context.DatabaseBackupHistory
            .Where(h => h.created_utc < threshold && h.status == "SUCCESS" && (h.operation_type == "MANUAL" || h.operation_type == "AUTOMATIC"))
            .ToListAsync(cancellationToken);

        foreach (var entry in oldEntries)
        {
            try
            {
                if (File.Exists(entry.file_path))
                {
                    File.Delete(entry.file_path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar el archivo de respaldo {Ruta}.", entry.file_path);
            }
        }

        if (oldEntries.Count > 0)
        {
            _context.DatabaseBackupHistory.RemoveRange(oldEntries);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> SupportsCompressionAsync(CancellationToken cancellationToken = default)
    {
        if (_supportsCompression.HasValue)
        {
            return _supportsCompression.Value;
        }

        try
        {
            await using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync(cancellationToken);

            const string query = "SELECT CAST(SERVERPROPERTY('Edition') AS NVARCHAR(128))";
            await using var command = new SqlCommand(query, connection);
            var edition = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? string.Empty;

            _supportsCompression = !edition.Contains("Express", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo determinar si el servidor soporta compresión. Se asumirá que no.");
            _supportsCompression = false;
        }

        return _supportsCompression.Value;
    }

    private void EnsureDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private string NormalizeDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = _options.DefaultDirectory;
        }

        return Path.GetFullPath(directory);
    }

    private async Task ExecuteBackupCommandAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        var supportsCompression = await SupportsCompressionAsync(cancellationToken);
        var compressionClause = supportsCompression ? ", COMPRESSION" : string.Empty;

        if (!supportsCompression)
        {
            _logger.LogInformation("La edición de SQL Server no soporta compresión. Se generará el respaldo sin COMPRESSION.");
        }

        var commandText = $@"
BACKUP DATABASE [{_databaseName}] TO DISK = @path WITH INIT{compressionClause}, STATS = 5;";

        await using var command = new SqlCommand(commandText, connection)
        {
            CommandTimeout = (int)TimeSpan.FromMinutes(10).TotalSeconds
        };
        command.Parameters.AddWithValue("@path", filePath);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ExecuteRestoreCommandAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        var commandText = $@"
ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [{_databaseName}] FROM DISK = @path WITH REPLACE, STATS = 5;
ALTER DATABASE [{_databaseName}] SET MULTI_USER;";

        await using var command = new SqlCommand(commandText, connection)
        {
            CommandTimeout = (int)TimeSpan.FromMinutes(15).TotalSeconds
        };
        command.Parameters.AddWithValue("@path", filePath);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

