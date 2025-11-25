using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitaFacil.Migrations
{
    /// <inheritdoc />
    public partial class ClearCitasTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [Citas];");
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE NAME = 'id' AND OBJECT_ID = OBJECT_ID('[Citas]')) DBCC CHECKIDENT ('[Citas]', RESEED, 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se restauran los registros eliminados intencionalmente
        }
    }
}
