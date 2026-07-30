using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitOps.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShipments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Origin = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Destination = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PlannedPickupAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlannedDeliveryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstimatedLoad = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipments", x => x.Id);
                    table.CheckConstraint("ck_shipments_planned_dates", "\"PlannedDeliveryAt\" IS NULL OR \"PlannedDeliveryAt\" >= \"PlannedPickupAt\"");
                    table.ForeignKey(
                        name: "FK_shipments_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipments_drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipments_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shipments_CustomerId",
                table: "shipments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_DriverId",
                table: "shipments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_PlannedPickupAt",
                table: "shipments",
                column: "PlannedPickupAt");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_Reference",
                table: "shipments",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipments_Status",
                table: "shipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_VehicleId",
                table: "shipments",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipments");
        }
    }
}
