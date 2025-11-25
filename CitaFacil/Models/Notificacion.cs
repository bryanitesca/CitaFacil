using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitaFacil.Models
{
    public class Notificacion
    {
        [Key]
        public long id { get; set; }

        [ForeignKey("Doctor")]
        public long doctor_id { get; set; }

        [ForeignKey("Paciente")]
        public long paciente_id { get; set; }

        [Required]
        [StringLength(120)]
        public string asunto { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string mensaje { get; set; } = string.Empty;

        public bool via_sistema { get; set; } = true;

        public bool leida { get; set; } = false;

        [DataType(DataType.DateTime)]
        public DateTime enviada_el { get; set; } = DateTime.UtcNow;

        public virtual Doctor? Doctor { get; set; }

        public virtual Paciente? Paciente { get; set; }
    }
}

