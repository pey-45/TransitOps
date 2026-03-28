using System;
using Microsoft.EntityFrameworkCore.Migrations;
using TransitOps.Api.Domain.Enums;

#nullable disable

namespace TransitOps.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:shipment_event_type", "assigned,cancelled,checkpoint,created,delivered,departed,incident")
                .Annotation("Npgsql:Enum:shipment_event_type.shipment_event_type", "created,assigned,departed,checkpoint,incident,delivered,cancelled")
                .Annotation("Npgsql:Enum:transport_status", "cancelled,delivered,in_transit,planned")
                .Annotation("Npgsql:Enum:transport_status.transport_status", "planned,in_transit,delivered,cancelled")
                .Annotation("Npgsql:Enum:user_role", "admin,operator")
                .Annotation("Npgsql:Enum:user_role.user_role", "admin,operator")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION set_updated_at()
                RETURNS TRIGGER
                AS $$
                BEGIN
                    NEW.updated_at = CURRENT_TIMESTAMP;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    user_role = table.Column<UserRole>(type: "user_role", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "driver",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    license_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    license_expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    plate_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    internal_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    capacity_kg = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    capacity_m3 = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle", x => x.id);
                    table.CheckConstraint("ck_vehicle_capacity_kg_non_negative", "\"capacity_kg\" IS NULL OR \"capacity_kg\" >= 0");
                    table.CheckConstraint("ck_vehicle_capacity_m3_non_negative", "\"capacity_m3\" IS NULL OR \"capacity_m3\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "transport",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    origin = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    destination = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    planned_pickup_at = table.Column<DateTime>(type: "timestamp", nullable: false),
                    planned_delivery_at = table.Column<DateTime>(type: "timestamp", nullable: true),
                    actual_pickup_at = table.Column<DateTime>(type: "timestamp", nullable: true),
                    actual_delivery_at = table.Column<DateTime>(type: "timestamp", nullable: true),
                    status = table.Column<TransportStatus>(type: "transport_status", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transport", x => x.id);
                    table.CheckConstraint("ck_transport_actual_dates", "\"actual_pickup_at\" IS NULL OR \"actual_delivery_at\" IS NULL OR \"actual_delivery_at\" >= \"actual_pickup_at\"");
                    table.CheckConstraint("ck_transport_planned_dates", "\"planned_delivery_at\" IS NULL OR \"planned_delivery_at\" >= \"planned_pickup_at\"");
                    table.ForeignKey(
                        name: "fk_transport_driver",
                        column: x => x.driver_id,
                        principalTable: "driver",
                        principalColumn: "id",
                        onUpdate: ReferentialAction.Cascade,
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_transport_vehicle",
                        column: x => x.vehicle_id,
                        principalTable: "vehicle",
                        principalColumn: "id",
                        onUpdate: ReferentialAction.Cascade,
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "shipment_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    transport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<ShipmentEventType>(type: "shipment_event_type", nullable: false),
                    event_date = table.Column<DateTime>(type: "timestamp", nullable: false),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_event", x => x.id);
                    table.ForeignKey(
                        name: "fk_shipment_event_created_by_user",
                        column: x => x.created_by_user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onUpdate: ReferentialAction.Cascade,
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shipment_event_transport",
                        column: x => x.transport_id,
                        principalTable: "transport",
                        principalColumn: "id",
                        onUpdate: ReferentialAction.Cascade,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_user_deleted_at",
                table: "app_user",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_app_user_email_active",
                table: "app_user",
                column: "email",
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_app_user_username_active",
                table: "app_user",
                column: "username",
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_driver_deleted_at",
                table: "driver",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_driver_email_active",
                table: "driver",
                column: "email",
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_driver_employee_code_active",
                table: "driver",
                column: "employee_code",
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_driver_license_number_active",
                table: "driver",
                column: "license_number",
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_shipment_event_created_by_user_id",
                table: "shipment_event",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipment_event_date",
                table: "shipment_event",
                column: "event_date");

            migrationBuilder.CreateIndex(
                name: "ix_shipment_event_deleted_at",
                table: "shipment_event",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_shipment_event_transport_id",
                table: "shipment_event",
                column: "transport_id");

            migrationBuilder.CreateIndex(
                name: "ix_transport_deleted_at",
                table: "transport",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_transport_driver_id",
                table: "transport",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_transport_status",
                table: "transport",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_transport_vehicle_id",
                table: "transport",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ux_transport_reference_active",
                table: "transport",
                column: "reference",
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_deleted_at",
                table: "vehicle",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ux_vehicle_internal_code_active",
                table: "vehicle",
                column: "internal_code",
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_vehicle_plate_number_active",
                table: "vehicle",
                column: "plate_number",
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.Sql(
                """
                CREATE TRIGGER trg_set_updated_at_app_user
                BEFORE UPDATE ON app_user
                FOR EACH ROW
                EXECUTE FUNCTION set_updated_at();

                CREATE TRIGGER trg_set_updated_at_driver
                BEFORE UPDATE ON driver
                FOR EACH ROW
                EXECUTE FUNCTION set_updated_at();

                CREATE TRIGGER trg_set_updated_at_vehicle
                BEFORE UPDATE ON vehicle
                FOR EACH ROW
                EXECUTE FUNCTION set_updated_at();

                CREATE TRIGGER trg_set_updated_at_transport
                BEFORE UPDATE ON transport
                FOR EACH ROW
                EXECUTE FUNCTION set_updated_at();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_set_updated_at_transport ON transport;
                DROP TRIGGER IF EXISTS trg_set_updated_at_vehicle ON vehicle;
                DROP TRIGGER IF EXISTS trg_set_updated_at_driver ON driver;
                DROP TRIGGER IF EXISTS trg_set_updated_at_app_user ON app_user;
                """);

            migrationBuilder.DropTable(
                name: "shipment_event");

            migrationBuilder.DropTable(
                name: "app_user");

            migrationBuilder.DropTable(
                name: "transport");

            migrationBuilder.DropTable(
                name: "driver");

            migrationBuilder.DropTable(
                name: "vehicle");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS set_updated_at();");
        }
    }
}
