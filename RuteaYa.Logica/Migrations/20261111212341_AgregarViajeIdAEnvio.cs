using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuteaYa.Logica.Migrations
{
    /// <inheritdoc />
    public partial class AgregarViajeIdAEnvio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ViajeId",
                table: "Envios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Envios_ViajeId",
                table: "Envios",
                column: "ViajeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Envios_Viajes_ViajeId",
                table: "Envios",
                column: "ViajeId",
                principalTable: "Viajes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Envios_Viajes_ViajeId",
                table: "Envios");

            migrationBuilder.DropIndex(
                name: "IX_Envios_ViajeId",
                table: "Envios");

            migrationBuilder.DropColumn(
                name: "ViajeId",
                table: "Envios");
        }
    }
}
