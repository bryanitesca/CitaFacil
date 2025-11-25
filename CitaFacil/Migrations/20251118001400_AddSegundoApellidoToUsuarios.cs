using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitaFacil.Migrations
{
    /// <inheritdoc />
    public partial class AddSegundoApellidoToUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "segundo_apellido",
                table: "Usuarios",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE U
                SET segundo_apellido = COALESCE(P.segundo_apellido, D.segundo_apellido)
                FROM Usuarios U
                LEFT JOIN Pacientes P ON U.id = P.usuario_id
                LEFT JOIN Doctores D ON U.id = D.usuario_id
                WHERE U.segundo_apellido IS NULL
                  AND (P.segundo_apellido IS NOT NULL OR D.segundo_apellido IS NOT NULL);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "segundo_apellido",
                table: "Usuarios");
        }
    }
}
