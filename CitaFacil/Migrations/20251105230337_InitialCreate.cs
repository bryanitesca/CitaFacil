using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitaFacil.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Especialidades",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    icono = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    activa = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Especialidades", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    apellido = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    usuario = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    contraseña = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    rol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    correo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    celular = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    foto_url = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    creado_el = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Doctores",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    segundo_apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    licencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    consultorio = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    biografia = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    especialidad_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctores", x => x.id);
                    table.ForeignKey(
                        name: "FK_Doctores_Especialidades_especialidad_id",
                        column: x => x.especialidad_id,
                        principalTable: "Especialidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Doctores_Usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "Usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pacientes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    segundo_apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    fecha_nacimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    genero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    direccion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.id);
                    table.ForeignKey(
                        name: "FK_Pacientes_Usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "Usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Citas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    hora = table.Column<TimeSpan>(type: "time", nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ubicacion = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    es_virtual = table.Column<bool>(type: "bit", nullable: false),
                    duracion_minutos = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "PENDIENTE"),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    creada_el = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    actualizada_el = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citas", x => x.id);
                    table.ForeignKey(
                        name: "FK_Citas_Doctores_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "Doctores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_Pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "Pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notificaciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    doctor_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    asunto = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    mensaje = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    via_sistema = table.Column<bool>(type: "bit", nullable: false),
                    leida = table.Column<bool>(type: "bit", nullable: false),
                    enviada_el = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificaciones", x => x.id);
                    table.ForeignKey(
                        name: "FK_notificaciones_Doctores_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "Doctores",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notificaciones_Pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "Pacientes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_doctor_id",
                table: "Citas",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_paciente_id",
                table: "Citas",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_Doctores_especialidad_id",
                table: "Doctores",
                column: "especialidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_Doctores_licencia",
                table: "Doctores",
                column: "licencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctores_usuario_id",
                table: "Doctores",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_doctor_id",
                table: "notificaciones",
                column: "doctor_id");

            migrationBuilder.CreateIndex(
                name: "IX_notificaciones_paciente_id",
                table: "notificaciones",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_usuario_id",
                table: "Pacientes",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_correo",
                table: "Usuarios",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_usuario",
                table: "Usuarios",
                column: "usuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Citas");

            migrationBuilder.DropTable(
                name: "notificaciones");

            migrationBuilder.DropTable(
                name: "Doctores");

            migrationBuilder.DropTable(
                name: "Pacientes");

            migrationBuilder.DropTable(
                name: "Especialidades");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
