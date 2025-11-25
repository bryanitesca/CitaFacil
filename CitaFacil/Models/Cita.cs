using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CitaFacil.Models
{
    public class Cita
    {
        [Key]
        public long id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime fecha { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan hora { get; set; }

        [StringLength(500)]
        public string? motivo { get; set; }

        [StringLength(500)]
        public string? diagnostico { get; set; }

        [StringLength(500)]
        public string? tratamiento_recomendado { get; set; }

        [StringLength(500)]
        public string? notas { get; set; }

        [StringLength(500)]
        public string? motivo_cancelacion { get; set; }

        public bool es_virtual { get; set; } = false;

        [Range(15, 240, ErrorMessage = "La duración debe estar entre 15 y 240 minutos.")]
        public int duracion_minutos { get; set; } = 30;

        [Required]
        [StringLength(50)]
        public string estado { get; set; } = "PENDIENTE"; // Valor por defecto

        [Required]
        [ForeignKey("Paciente")]
        public long paciente_id { get; set; }

        [Required]
        [ForeignKey("Doctor")]
        public long doctor_id { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime creada_el { get; set; } = DateTime.UtcNow;

        [DataType(DataType.DateTime)]
        public DateTime? actualizada_el { get; set; }

        // Propiedades de navegación
        public virtual Paciente Paciente { get; set; } = null!;
        public virtual Doctor Doctor { get; set; } = null!;

        // Métodos de validación del negocio
        public bool EsFechaValida()
        {
            var fechaHora = fecha.Add(hora);
            return fechaHora > DateTime.Now;
        }

        public bool EsHorarioLaboral()
        {
            var diaSemana = fecha.DayOfWeek;
            return diaSemana != DayOfWeek.Sunday && hora.Hours >= 8 && hora.Hours <= 17;
        }

        public bool PuedeSerCancelada()
        {
            var fechaHora = fecha.Add(hora);
            return estado == "PROGRAMADA" && fechaHora > DateTime.Now.AddHours(24);
        }

        public bool PuedeSerReprogramada()
        {
            var fechaHora = fecha.Add(hora);
            return estado == "PROGRAMADA" && fechaHora > DateTime.Now.AddHours(2);
        }
    }
}
