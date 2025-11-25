using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitaFacil.Models;

[Table("database_backup_configs")]
public class DatabaseBackupConfig
{
    [Key]
    public int id { get; set; }

    [Required]
    [MaxLength(260)]
    public string backup_directory { get; set; } = string.Empty;

    public bool auto_backup_enabled { get; set; } = true;

    [Required]
    public TimeSpan auto_backup_time { get; set; } = new TimeSpan(2, 0, 0);

    public int retention_days { get; set; } = 30;

    public DateTime? last_backup_utc { get; set; }

    public DateTime? last_automatic_backup_utc { get; set; }
}

