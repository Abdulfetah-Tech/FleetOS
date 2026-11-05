using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthioFleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "ak_vehicle_assignments_organization_id_id",
                table: "vehicle_assignments",
                columns: new[] { "organization_id", "id" });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    destination = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    planned_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actual_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    start_mileage = table.Column<decimal>(type: "numeric(12,1)", precision: 12, scale: 1, nullable: false),
                    end_mileage = table.Column<decimal>(type: "numeric(12,1)", precision: 12, scale: 1, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trips", x => x.id);
                    table.ForeignKey(
                        name: "fk_trips_drivers_organization_id_driver_id",
                        columns: x => new { x.organization_id, x.driver_id },
                        principalTable: "drivers",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trips_vehicle_assignments_organization_id_vehicle_assignmen",
                        columns: x => new { x.organization_id, x.vehicle_assignment_id },
                        principalTable: "vehicle_assignments",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trips_vehicles_organization_id_vehicle_id",
                        columns: x => new { x.organization_id, x.vehicle_id },
                        principalTable: "vehicles",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_trips_organization_id_driver_id",
                table: "trips",
                columns: new[] { "organization_id", "driver_id" });

            migrationBuilder.CreateIndex(
                name: "ix_trips_organization_id_status_planned_start",
                table: "trips",
                columns: new[] { "organization_id", "status", "planned_start" });

            migrationBuilder.CreateIndex(
                name: "ix_trips_organization_id_vehicle_assignment_id",
                table: "trips",
                columns: new[] { "organization_id", "vehicle_assignment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_trips_organization_id_vehicle_id_planned_start",
                table: "trips",
                columns: new[] { "organization_id", "vehicle_id", "planned_start" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_vehicle_assignments_organization_id_id",
                table: "vehicle_assignments");
        }
    }
}
