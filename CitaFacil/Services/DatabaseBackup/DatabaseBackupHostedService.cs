using CitaFacil.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CitaFacil.Services.DatabaseBackup;

public class DatabaseBackupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseBackupHostedService> _logger;

    public DatabaseBackupHostedService(IServiceScopeFactory scopeFactory, ILogger<DatabaseBackupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de respaldos automaticos inicializado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EjecutarCicloAsync(stoppingToken);
            }
            catch (Exception ex)
            {
            _logger.LogError(ex, "Error en el servicio de respaldos automaticos.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task EjecutarCicloAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var backupService = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();

        DatabaseBackupConfig? config = null;
        try
        {
            config = await backupService.GetOrCreateConfigurationAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            _logger.LogInformation("La tabla de configuracion de respaldos aun no existe. Aplica las migraciones pendientes para habilitar los respaldos automaticos.");
            return;
        }

        if (config == null || !config.auto_backup_enabled)
        {
            return;
        }

        var ahoraLocal = DateTime.Now;
        var horaObjetivo = config.auto_backup_time;
        var ultimaEjecucion = config.last_automatic_backup_utc?.ToLocalTime();

        var debeEjecutarHoy = ahoraLocal.TimeOfDay >= horaObjetivo;
        var yaSeEjecutoHoy = ultimaEjecucion.HasValue && ultimaEjecucion.Value.Date == ahoraLocal.Date;

        if (debeEjecutarHoy && !yaSeEjecutoHoy)
        {
            _logger.LogInformation("Iniciando respaldo automatico programado.");
            await backupService.CreateBackupAsync(true, cancellationToken);
        }
    }
}

