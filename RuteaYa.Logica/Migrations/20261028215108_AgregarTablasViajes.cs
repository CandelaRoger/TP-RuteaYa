using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RuteaYa.Logica.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablasViajes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Viajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    OrigenDir = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrigenLat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    OrigenLng = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    DestinoDir = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DestinoLat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    DestinoLng = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    HoraSalida = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraLlegada = table.Column<TimeSpan>(type: "time", nullable: false),
                    MaxPaquetes = table.Column<int>(type: "int", nullable: false),
                    PesoMaxKg = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    DesvioMaxKm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    DistanciaKm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    DuracionMin = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Viajes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Viajes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Paradas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ViajeId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Lat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Lng = table.Column<decimal>(type: "decimal(9,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paradas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paradas_Viajes_ViajeId",
                        column: x => x.ViajeId,
                        principalTable: "Viajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViajeDias",
                columns: table => new
                {
                    ViajeId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViajeDias", x => new { x.ViajeId, x.DiaSemana });
                    table.ForeignKey(
                        name: "FK_ViajeDias_Viajes_ViajeId",
                        column: x => x.ViajeId,
                        principalTable: "Viajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Paradas_ViajeId",
                table: "Paradas",
                column: "ViajeId");

            migrationBuilder.CreateIndex(
                name: "IX_Viajes_UsuarioId",
                table: "Viajes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Paradas");

            migrationBuilder.DropTable(
                name: "ViajeDias");

            migrationBuilder.DropTable(
                name: "Viajes");
        }
    }
}
