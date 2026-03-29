using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitOps.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QualifyEnumColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "transport",
                type: "public.transport_status",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "transport_status");

            migrationBuilder.AlterColumn<int>(
                name: "event_type",
                table: "shipment_event",
                type: "public.shipment_event_type",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "shipment_event_type");

            migrationBuilder.AlterColumn<int>(
                name: "user_role",
                table: "app_user",
                type: "public.user_role",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "user_role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "transport",
                type: "transport_status",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.transport_status");

            migrationBuilder.AlterColumn<int>(
                name: "event_type",
                table: "shipment_event",
                type: "shipment_event_type",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.shipment_event_type");

            migrationBuilder.AlterColumn<int>(
                name: "user_role",
                table: "app_user",
                type: "user_role",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "public.user_role");
        }
    }
}
