using System.ComponentModel.DataAnnotations;
using CitaFacil.Models;

namespace CitaFacil.ViewModels.Admin;

public class DatabaseBackupConfigViewModel
{
    [Required]
    [Display(Name = "Carpeta de respaldos")]
    public string BackupDirectory { get; set; } = string.Empty;

    [Display(Name = "Respaldo automático habilitado")]
    public bool AutoBackupEnabled { get; set; } = true;

    [Required]
    [RegularExpression("^([01]?\\d|2[0-3]):[0-5]\\d$", ErrorMessage = "Formato de hora inválido (HH:mm)")]
    [Display(Name = "Hora del respaldo automático (HH:mm)")]
    public string AutoBackupTime { get; set; } = "02:00";

    [Display(Name = "Retención (días)")]
    [Range(0, 30, ErrorMessage = "La retención debe estar entre 0 y 30 días.")]
    public int RetentionDays { get; set; } = 30;

    public DateTime? LastBackupUtc { get; set; }
    public DateTime? LastAutomaticBackupUtc { get; set; }

    public static DatabaseBackupConfigViewModel FromEntity(DatabaseBackupConfig entity)
    {
        return new DatabaseBackupConfigViewModel
        {
            BackupDirectory = entity.backup_directory,
            AutoBackupEnabled = entity.auto_backup_enabled,
            AutoBackupTime = $"{(int)entity.auto_backup_time.TotalHours:00}:{entity.auto_backup_time.Minutes:00}",
            RetentionDays = entity.retention_days,
            LastBackupUtc = entity.last_backup_utc,
            LastAutomaticBackupUtc = entity.last_automatic_backup_utc
        };
    }
}

