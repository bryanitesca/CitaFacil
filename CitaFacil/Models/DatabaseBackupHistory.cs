using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitaFacil.Models;

[Table("database_backup_history")]
public class DatabaseBackupHistory
{
    [Key]
    public long id { get; set; }

    [Required]
    [MaxLength(80)]
    public string operation_type { get; set; } = "MANUAL"; // MANUAL / AUTOMATIC / RESTORE

    [Required]
    [MaxLength(120)]
    public string status { get; set; } = "SUCCESS"; // SUCCESS / FAILED

    [Required]
    [MaxLength(260)]
    public string file_path { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string file_name { get; set; } = string.Empty;

    [Required]
    public DateTime created_utc { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? message { get; set; }
}

