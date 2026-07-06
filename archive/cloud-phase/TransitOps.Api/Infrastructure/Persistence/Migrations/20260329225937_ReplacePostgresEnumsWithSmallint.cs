using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitOps.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePostgresEnumsWithSmallint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE transport
                ALTER COLUMN status TYPE smallint
                USING CASE status::text
                    WHEN 'planned' THEN 0
                    WHEN 'in_transit' THEN 1
                    WHEN 'delivered' THEN 2
                    WHEN 'cancelled' THEN 3
                END;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE shipment_event
                ALTER COLUMN event_type TYPE smallint
                USING CASE event_type::text
                    WHEN 'created' THEN 0
                    WHEN 'assigned' THEN 1
                    WHEN 'departed' THEN 2
                    WHEN 'checkpoint' THEN 3
                    WHEN 'incident' THEN 4
                    WHEN 'delivered' THEN 5
                    WHEN 'cancelled' THEN 6
                END;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE app_user
                ALTER COLUMN user_role TYPE smallint
                USING CASE user_role::text
                    WHEN 'admin' THEN 0
                    WHEN 'operator' THEN 1
                END;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_transport_status_valid",
                table: "transport",
                sql: "\"status\" IN (0, 1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_shipment_event_type_valid",
                table: "shipment_event",
                sql: "\"event_type\" IN (0, 1, 2, 3, 4, 5, 6)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_app_user_role_valid",
                table: "app_user",
                sql: "\"user_role\" IN (0, 1)");

            migrationBuilder.Sql("DROP TYPE IF EXISTS public.transport_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS public.shipment_event_type;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS public.user_role;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_transport_status_valid",
                table: "transport");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shipment_event_type_valid",
                table: "shipment_event");

            migrationBuilder.DropCheckConstraint(
                name: "ck_app_user_role_valid",
                table: "app_user");

            migrationBuilder.Sql("CREATE TYPE public.transport_status AS ENUM ('planned', 'in_transit', 'delivered', 'cancelled');");
            migrationBuilder.Sql("CREATE TYPE public.shipment_event_type AS ENUM ('created', 'assigned', 'departed', 'checkpoint', 'incident', 'delivered', 'cancelled');");
            migrationBuilder.Sql("CREATE TYPE public.user_role AS ENUM ('admin', 'operator');");

            migrationBuilder.Sql(
                """
                ALTER TABLE transport
                ALTER COLUMN status TYPE public.transport_status
                USING CASE status
                    WHEN 0 THEN 'planned'
                    WHEN 1 THEN 'in_transit'
                    WHEN 2 THEN 'delivered'
                    WHEN 3 THEN 'cancelled'
                END::public.transport_status;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE shipment_event
                ALTER COLUMN event_type TYPE public.shipment_event_type
                USING CASE event_type
                    WHEN 0 THEN 'created'
                    WHEN 1 THEN 'assigned'
                    WHEN 2 THEN 'departed'
                    WHEN 3 THEN 'checkpoint'
                    WHEN 4 THEN 'incident'
                    WHEN 5 THEN 'delivered'
                    WHEN 6 THEN 'cancelled'
                END::public.shipment_event_type;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE app_user
                ALTER COLUMN user_role TYPE public.user_role
                USING CASE user_role
                    WHEN 0 THEN 'admin'
                    WHEN 1 THEN 'operator'
                END::public.user_role;
                """);
        }
    }
}
