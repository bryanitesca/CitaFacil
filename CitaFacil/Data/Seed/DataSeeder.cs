using CitaFacil.Constants;
using CitaFacil.Models;
using CitaFacil.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace CitaFacil.Data.Seed
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            try
            {
                // Solo ejecutar si no hay datos
                if (context.Usuarios.Any())
                    return;

                // Seed data in correct order to handle relationships
                await SeedEspecialidades(context);
                await SeedUsuarios(context);
                await SeedDoctores(context);
                await SeedPacientes(context);
                await SeedCitas(context);
                await SeedNotificaciones(context);
                
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error seeding database: {ex.Message}", ex);
            }
        }

        private static async Task SeedEspecialidades(ApplicationDbContext context)
        {
            var especialidades = new List<Especialidad>
            {
                new Especialidad
                {
                    nombre = "Medicina General",
                    descripcion = "Atención médica integral y preventiva para toda la familia"
                },
                new Especialidad
                {
                    nombre = "Cardiología",
                    descripcion = "Diagnóstico y tratamiento de enfermedades del corazón y sistema cardiovascular"
                },
                new Especialidad
                {
                    nombre = "Pediatría",
                    descripcion = "Atención médica especializada para bebés, niños y adolescentes"
                },
                new Especialidad
                {
                    nombre = "Dermatología",
                    descripcion = "Diagnóstico y tratamiento de enfermedades de la piel, cabello y uñas"
                },
                new Especialidad
                {
                    nombre = "Ginecología",
                    descripcion = "Atención médica especializada en salud femenina y reproductiva"
                }
            };

            context.Especialidades.AddRange(especialidades);
            await context.SaveChangesAsync();
        }

        private static async Task SeedUsuarios(ApplicationDbContext context)
        {
            var usuarios = new List<Usuario>
            {
                // 1 Administrador
                new Usuario
                {
                    usuario = "admin",
                    correo = "admin@citafacil.com",
                    nombre = "Carlos",
                    apellido = "Administrador",
                    segundo_apellido = null,
                    celular = "5551234567",
                    rol = Roles.Administrador,
                    contraseña = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    activo = true,
                    creado_el = DateTime.UtcNow.AddMonths(-6),
                    foto_url = null
                },
                
                // 3 Doctores
                new Usuario
                {
                    usuario = "dr.martinez",
                    correo = "martinez@citafacil.com",
                    nombre = "Ana",
                    apellido = "Martínez",
                    segundo_apellido = "López",
                    celular = "5552345678",
                    rol = Roles.Doctor,
                    contraseña = BCrypt.Net.BCrypt.HashPassword("Doctor123!"),
                    activo = true,
                    creado_el = DateTime.UtcNow.AddMonths(-4),
                    foto_url = "https://ui-avatars.com/api/?name=Ana+Martinez&background=135bec&color=fff&size=200"
                },
                new Usuario
                {
                    usuario = "dr.rodriguez",
                    correo = "rodriguez@citafacil.com",
                    nombre = "Luis",
                    apellido = "Rodríguez",
                    segundo_apellido = "Hernández",
                    celular = "5553456789",
                    rol = Roles.Doctor,
                    contraseña = BCrypt.Net.BCrypt.HashPassword("Doctor123!"),
                    activo = true,
                    creado_el = DateTime.UtcNow.AddMonths(-3),
                    foto_url = "https://ui-avatars.com/api/?name=Luis+Rodriguez&background=135bec&color=fff&size=200"
                },
                new Usuario
                {
                    usuario = "dr.garcia",
                    correo = "garcia@citafacil.com",
                    nombre = "María",
                    apellido = "García",
                    segundo_apellido = "Mendoza",
                    celular = "5554567890",
                    rol = Roles.Doctor,
                    contraseña = BCrypt.Net.BCrypt.HashPassword("Doctor123!"),
                    activo = true,
                    creado_el = DateTime.UtcNow.AddMonths(-2),
                    foto_url = "https://ui-avatars.com/api/?name=Maria+Garcia&background=135bec&color=fff&size=200"
                },
                
                // 5 Pacientes
                new Usuario
                {
                    usuario = "juan.perez",
                    correo = "juan.perez@email.com",
                    nombre = "Juan",
                    apellido = "Pérez",
                    segundo_apellido = "González",
                    celular = "5555678901",
                    rol = Roles.Paciente,
                    contraseña = BCrypt.Net.BCrypt.HashPassword("Paciente123!"),
                    activo = true,
                    creado_el = DateTime.UtcNow.AddMonths(-1),
                    foto_url = null
                },
                new Usuario
                {
                    usuario = "maria.lopez",
                    correo = "maria.lopez@email.com",
                    nombre = "María",
                    apellido = "López",
                    segundo_apellido = "Martín",
                    celular = "5556789012",
                    rol = Roles.Paciente,
                    contraseña = BCrypt.Net.BCrypt.HashPassword("Paciente123!"),
                    activo = true,
                    creado_el = DateTime.UtcNow.AddDays(-45),
                    foto_url = null
                },
                new Usuario
                {
                    usuario = "carlos.gomez",
                    correo = "carlos.gomez@email.com",
                    nombre = "Carlos",
                    apellido = "Gómez",
                    segundo_apellido = "Ruiz",
                    celular = "5557890123",
                    rol = Roles.Paciente,
                    contraseña = BCrypt.Net.BCrypt.HashPassword("Paciente123!"),
                    activo = true,
                    creado_el = DateTime.UtcNow.AddDays(-30),
                    foto_url = null
                },
                new Usuario
                {
                    usuario = "ana.torres",
                    correo = "ana.torres@email.com",
                    nombre = "Ana",
                    apellido = "Torres",
                    segundo_apellido = "Vega",
                    celular = "5558901234",
                    rol = Roles.Paciente,
                    contraseña = BCrypt.Net.BCrypt.HashPassword("Paciente123!"),
                    activo = true,
                    creado_el = DateTime.UtcNow.AddDays(-20),
                    foto_url = null
                },
                new Usuario
                {
                    usuario = "pedro.sanchez",
                    correo = "pedro.sanchez@email.com",
                    nombre = "Pedro",
                    apellido = "Sánchez",
                    segundo_apellido = "Morales",
                    celular = "5559012345",
                    rol = Roles.Paciente,
                    contraseña = BCrypt.Net.BCrypt.HashPassword("Paciente123!"),
                    activo = true,
                    creado_el = DateTime.UtcNow.AddDays(-10),
                    foto_url = null
                }
            };

            context.Usuarios.AddRange(usuarios);
            await context.SaveChangesAsync();
        }

        private static async Task SeedDoctores(ApplicationDbContext context)
        {
            // Obtener especialidades y usuarios doctores
            var especialidades = await context.Especialidades.ToListAsync();
            var usuariosDoctores = await context.Usuarios.Where(u => u.rol == Roles.Doctor).ToListAsync();

            var doctores = new List<Doctor>
            {
                new Doctor
                {
                    usuario_id = usuariosDoctores[0].id, // Dr. Martinez
                    nombre = "Ana",
                    apellido = "Martínez",
                    segundo_apellido = "López",
                    especialidad_id = especialidades.First(e => e.nombre == "Cardiología").id,
                    licencia = "MED001234",
                    consultorio = "Consultorio 101",
                    biografia = "Cardióloga con 8 años de experiencia"
                },
                new Doctor
                {
                    usuario_id = usuariosDoctores[1].id, // Dr. Rodriguez
                    nombre = "Luis",
                    apellido = "Rodríguez",
                    segundo_apellido = "Hernández",
                    especialidad_id = especialidades.First(e => e.nombre == "Pediatría").id,
                    licencia = "MED005678",
                    consultorio = "Consultorio 102",
                    biografia = "Pediatra especializado en medicina infantil"
                },
                new Doctor
                {
                    usuario_id = usuariosDoctores[2].id, // Dr. Garcia
                    nombre = "María",
                    apellido = "García",
                    segundo_apellido = "Mendoza",
                    especialidad_id = especialidades.First(e => e.nombre == "Ginecología").id,
                    licencia = "MED009012",
                    consultorio = "Consultorio 205",
                    biografia = "Ginecóloga con enfoque en salud reproductiva"
                }
            };

            context.Doctores.AddRange(doctores);
            await context.SaveChangesAsync();
        }

        private static async Task SeedPacientes(ApplicationDbContext context)
        {
            var usuariosPacientes = await context.Usuarios.Where(u => u.rol == Roles.Paciente).ToListAsync();

            var pacientes = new List<Paciente>
            {
                new Paciente
                {
                    usuario_id = usuariosPacientes[0].id, // Juan Perez
                    nombre = "Juan",
                    apellido = "Pérez",
                    segundo_apellido = "González",
                    fecha_nacimiento = new DateTime(1985, 3, 15),
                    genero = "Masculino",
                    direccion = "Av. Reforma 123, Col. Centro, CDMX"
                },
                new Paciente
                {
                    usuario_id = usuariosPacientes[1].id, // Maria Lopez
                    nombre = "María",
                    apellido = "López",
                    segundo_apellido = "Martín",
                    fecha_nacimiento = new DateTime(1990, 7, 22),
                    genero = "Femenino",
                    direccion = "Calle Madero 456, Col. Juárez, CDMX"
                },
                new Paciente
                {
                    usuario_id = usuariosPacientes[2].id, // Carlos Gomez
                    nombre = "Carlos",
                    apellido = "Gómez",
                    segundo_apellido = "Ruiz",
                    fecha_nacimiento = new DateTime(1978, 11, 8),
                    genero = "Masculino",
                    direccion = "Insurgentes Sur 789, Col. Roma, CDMX"
                },
                new Paciente
                {
                    usuario_id = usuariosPacientes[3].id, // Ana Torres
                    nombre = "Ana",
                    apellido = "Torres",
                    segundo_apellido = "Vega",
                    fecha_nacimiento = new DateTime(1995, 1, 30),
                    genero = "Femenino",
                    direccion = "Eje Central 321, Col. Doctores, CDMX"
                },
                new Paciente
                {
                    usuario_id = usuariosPacientes[4].id, // Pedro Sanchez
                    nombre = "Pedro",
                    apellido = "Sánchez",
                    segundo_apellido = "Morales",
                    fecha_nacimiento = new DateTime(1982, 9, 12),
                    genero = "Masculino",
                    direccion = "Paseo de la Reforma 654, Col. Polanco, CDMX"
                }
            };

            context.Pacientes.AddRange(pacientes);
            await context.SaveChangesAsync();
        }

        private static async Task SeedCitas(ApplicationDbContext context)
        {
            var doctores = await context.Doctores.Include(d => d.Usuario).ToListAsync();
            var pacientes = await context.Pacientes.Include(p => p.Usuario).ToListAsync();

            var citas = new List<Cita>
            {
                // Citas pasadas (completadas)
                new Cita
                {
                    paciente_id = pacientes[0].id, // Juan Perez
                    doctor_id = doctores[0].id,     // Dr. Martinez (Cardiología)
                    fecha = DateTime.Today.AddDays(-15),
                    hora = new TimeSpan(9, 0, 0),
                    motivo = "Consulta de revisión cardiológica",
                    estado = "COMPLETADA",
                    duracion_minutos = 30,
                    es_virtual = false,
                    notas = "Paciente presenta mejora en su condición cardiovascular. Se recomienda continuar con medicamento actual.",
                    creada_el = DateTime.UtcNow.AddDays(-20)
                },
                new Cita
                {
                    paciente_id = pacientes[1].id, // Maria Lopez
                    doctor_id = doctores[2].id,     // Dr. Garcia (Ginecología)
                    fecha = DateTime.Today.AddDays(-8),
                    hora = new TimeSpan(14, 30, 0),
                    motivo = "Control ginecológico anual",
                    estado = "COMPLETADA",
                    duracion_minutos = 45,
                    es_virtual = false,
                    notas = "Examen rutinario sin hallazgos significativos. Próxima cita en 12 meses.",
                    creada_el = DateTime.UtcNow.AddDays(-15)
                },
                
                // Citas próximas (confirmadas)
                new Cita
                {
                    paciente_id = pacientes[2].id, // Carlos Gomez
                    doctor_id = doctores[1].id,     // Dr. Rodriguez (Pediatría)
                    fecha = DateTime.Today.AddDays(3),
                    hora = new TimeSpan(10, 0, 0),
                    motivo = "Consulta para hijo menor",
                    estado = "CONFIRMADA",
                    duracion_minutos = 30,
                    es_virtual = false,
                    notas = "Revisar cartilla de vacunación del menor.",
                    creada_el = DateTime.UtcNow.AddDays(-2)
                },
                new Cita
                {
                    paciente_id = pacientes[3].id, // Ana Torres
                    doctor_id = doctores[0].id,     // Dr. Martinez (Cardiología)
                    fecha = DateTime.Today.AddDays(7),
                    hora = new TimeSpan(11, 30, 0),
                    motivo = "Primera consulta - evaluación cardiovascular",
                    estado = "PENDIENTE",
                    duracion_minutos = 60,
                    es_virtual = false,
                    notas = "Paciente joven con antecedentes familiares de problemas cardíacos.",
                    creada_el = DateTime.UtcNow.AddDays(-1)
                },
                new Cita
                {
                    paciente_id = pacientes[4].id, // Pedro Sanchez
                    doctor_id = doctores[2].id,     // Dr. Garcia (Ginecología)
                    fecha = DateTime.Today.AddDays(12),
                    hora = new TimeSpan(16, 0, 0),
                    motivo = "Consulta virtual - seguimiento",
                    estado = "CONFIRMADA",
                    duracion_minutos = 30,
                    es_virtual = true,
                    notas = "Seguimiento post-operatorio. Consulta virtual por comodidad del paciente.",
                    creada_el = DateTime.UtcNow
                }
            };

            context.Citas.AddRange(citas);
            await context.SaveChangesAsync();
        }

        private static async Task SeedNotificaciones(ApplicationDbContext context)
        {
            var doctores = await context.Doctores.Include(d => d.Usuario).ToListAsync();
            var pacientes = await context.Pacientes.Include(p => p.Usuario).ToListAsync();

            var notificaciones = new List<Notificacion>
            {
                new Notificacion
                {
                    doctor_id = doctores[0].id,
                    paciente_id = pacientes[0].id, // Juan Perez
                    asunto = "Cita confirmada",
                    mensaje = "Su cita con Dr. Martinez ha sido confirmada para el día de mañana a las 9:00 AM",
                    via_sistema = true,
                    leida = false,
                    enviada_el = DateTime.UtcNow.AddHours(-2)
                },
                new Notificacion
                {
                    doctor_id = doctores[2].id,
                    paciente_id = pacientes[1].id, // Maria Lopez
                    asunto = "Recordatorio de cita",
                    mensaje = "Recordatorio: Tiene una cita programada con Dr. Garcia en 2 días",
                    via_sistema = true,
                    leida = true,
                    enviada_el = DateTime.UtcNow.AddDays(-1)
                },
                new Notificacion
                {
                    doctor_id = doctores[0].id,
                    paciente_id = pacientes[3].id, // Ana Torres
                    asunto = "Nueva cita agendada",
                    mensaje = "Se ha agendado una nueva cita con el paciente Ana Torres para la próxima semana",
                    via_sistema = true,
                    leida = false,
                    enviada_el = DateTime.UtcNow.AddMinutes(-30)
                },
                new Notificacion
                {
                    doctor_id = doctores[1].id,
                    paciente_id = pacientes[2].id, // Carlos Gomez
                    asunto = "Cita reprogramada",
                    mensaje = "La cita con Carlos Gómez ha sido reprogramada para el viernes",
                    via_sistema = true,
                    leida = true,
                    enviada_el = DateTime.UtcNow.AddHours(-6)
                },
                new Notificacion
                {
                    doctor_id = doctores[0].id,
                    paciente_id = pacientes[4].id, // Pedro Sanchez
                    asunto = "Reporte diario",
                    mensaje = "Resumen del día: 12 citas programadas, 8 completadas, 2 canceladas",
                    via_sistema = true,
                    leida = false,
                    enviada_el = DateTime.UtcNow.AddHours(-1)
                }
            };

            context.Notificaciones.AddRange(notificaciones);
            await context.SaveChangesAsync();
        }
    }
}