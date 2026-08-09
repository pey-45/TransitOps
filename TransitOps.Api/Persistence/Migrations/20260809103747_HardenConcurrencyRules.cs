using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitOps.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenConcurrencyRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shipments_DriverId",
                table: "shipments");

            migrationBuilder.DropIndex(
                name: "IX_shipments_VehicleId",
                table: "shipments");

            migrationBuilder.CreateIndex(
                name: "UX_shipments_open_DriverId",
                table: "shipments",
                column: "DriverId",
                unique: true,
                filter: "\"DriverId\" IS NOT NULL AND \"Status\" IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "UX_shipments_open_VehicleId",
                table: "shipments",
                column: "VehicleId",
                unique: true,
                filter: "\"VehicleId\" IS NOT NULL AND \"Status\" IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_shipments_open_DriverId",
                table: "shipments");

            migrationBuilder.DropIndex(
                name: "UX_shipments_open_VehicleId",
                table: "shipments");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_DriverId",
                table: "shipments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_VehicleId",
                table: "shipments",
                column: "VehicleId");
        }
    }
}
