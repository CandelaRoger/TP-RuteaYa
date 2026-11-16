using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuteaYa.Logica.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDesvioAEnvio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DesvioKm",
                table: "Envios",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DesvioKm",
                table: "Envios");
        }
    }
}
