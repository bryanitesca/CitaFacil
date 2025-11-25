using CitaFacil.Models;
using Microsoft.EntityFrameworkCore;

namespace CitaFacil.Data
{
    public class ApplicationDbContext : DbContext
    {
        // El constructor que recibe las opciones de configuración (como la cadena de conexión)
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Mapea tus clases (Modelos) a tablas en la base de datos
        // (Clase singular, DbSet plural)
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Doctor> Doctores { get; set; }
        public DbSet<Especialidad> Especialidades { get; set; }
        public DbSet<Cita> Citas { get; set; }
        public virtual DbSet<Notificacion> Notificaciones { get; set; } = null!;
        public DbSet<DatabaseBackupConfig> DatabaseBackupConfigs { get; set; } = null!;
        public DbSet<DatabaseBackupHistory> DatabaseBackupHistory { get; set; } = null!;


        // (Opcional pero recomendado) Configuración de relaciones y constraints
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Configuración de la relación 1 a 1 entre Usuario y Paciente ---
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Paciente) // Un Usuario tiene un Paciente
                .WithOne(p => p.Usuario) // Un Paciente tiene un Usuario
                .HasForeignKey<Paciente>(p => p.usuario_id) // La FK está en Paciente
                .OnDelete(DeleteBehavior.Cascade);

            // --- Configuración de la relación 1 a 1 entre Usuario y Doctor ---
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Doctor) // Un Usuario tiene un Doctor
                .WithOne(d => d.Usuario) // Un Doctor tiene un Usuario
                .HasForeignKey<Doctor>(d => d.usuario_id) // La FK está en Doctor
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Paciente>()
                .HasIndex(p => p.usuario_id)
                .IsUnique();

            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.usuario_id)
                .IsUnique();

            // --- Configuración de la relación 1 a N entre Especialidad y Doctor ---
            modelBuilder.Entity<Especialidad>()
                .HasMany(e => e.Doctores) // Una Especialidad tiene muchos Doctores
                .WithOne(d => d.Especialidad) // Un Doctor tiene una Especialidad
                .HasForeignKey(d => d.especialidad_id); // La FK está en Doctor

            modelBuilder.Entity<Especialidad>()
                .Property(e => e.activa)
                .HasDefaultValue(true);

            // --- Configuración de la relación 1 a N entre Paciente y Cita ---
            modelBuilder.Entity<Paciente>()
                .HasMany(p => p.Citas) // Un Paciente tiene muchas Citas
                .WithOne(c => c.Paciente) // Una Cita tiene un Paciente
                .HasForeignKey(c => c.paciente_id) // La FK está en Cita
                .OnDelete(DeleteBehavior.Restrict); // Evita borrado en cascada (opcional)

            // --- Configuración de la relación 1 a N entre Doctor y Cita ---
            modelBuilder.Entity<Doctor>()
                .HasMany(d => d.Citas) // Un Doctor tiene muchas Citas
                .WithOne(c => c.Doctor) // Una Cita tiene un Doctor
                .HasForeignKey(c => c.doctor_id) // La FK está en Cita
                .OnDelete(DeleteBehavior.Restrict); // Evita borrado en cascada (opcional)

            // --- Asignar valor por defecto para Cita.estado ---
            modelBuilder.Entity<Cita>()
                .Property(c => c.estado)
                .HasDefaultValue("PENDIENTE");

            modelBuilder.Entity<Cita>()
                .Property(c => c.duracion_minutos)
                .HasDefaultValue(30);

            modelBuilder.Entity<Cita>()
                .Property(c => c.creada_el)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Usuario>()
                .Property(u => u.creado_el)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<Usuario>()
                .Property(u => u.activo)
                .HasDefaultValue(true);

            modelBuilder.Entity<Notificacion>(entity =>
            {
                entity.ToTable("notificaciones");
                entity.Property(e => e.asunto).HasMaxLength(120);
                entity.Property(e => e.mensaje).HasMaxLength(1000);
                entity.Property(e => e.enviada_el).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Doctor)
                    .WithMany()
                    .HasForeignKey(e => e.doctor_id)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Paciente)
                    .WithMany()
                    .HasForeignKey(e => e.paciente_id)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<DatabaseBackupConfig>()
                .Property(c => c.retention_days)
                .HasDefaultValue(30);

            modelBuilder.Entity<DatabaseBackupHistory>()
                .Property(h => h.created_utc)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
