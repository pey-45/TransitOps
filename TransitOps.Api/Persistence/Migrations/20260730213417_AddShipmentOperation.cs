using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitOps.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualDeliveryAt",
                table: "shipments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualPickupAt",
                table: "shipments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_shipments_actual_dates",
                table: "shipments",
                sql: "\"ActualDeliveryAt\" IS NULL OR \"ActualPickupAt\" IS NULL OR \"ActualDeliveryAt\" >= \"ActualPickupAt\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_shipments_actual_dates",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ActualDeliveryAt",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ActualPickupAt",
                table: "shipments");
        }
    }
}
