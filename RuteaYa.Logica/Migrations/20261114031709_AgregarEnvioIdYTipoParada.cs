using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuteaYa.Logica.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEnvioIdYTipoParada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnvioId",
                table: "Paradas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Paradas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnvioId",
                table: "Paradas");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Paradas");
        }
    }
}
