using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthioFleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    location = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    estimated_cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    actual_cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    resolution_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_incidents", x => x.id);
                    table.UniqueConstraint("ak_incidents_organization_id_id", x => new { x.organization_id, x.id });
                    table.ForeignKey(
                        name: "fk_incidents_drivers_organization_id_driver_id",
                        columns: x => new { x.organization_id, x.driver_id },
                        principalTable: "drivers",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_incidents_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_incidents_vehicles_organization_id_vehicle_id",
                        columns: x => new { x.organization_id, x.vehicle_id },
                        principalTable: "vehicles",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_organization_id_driver_id",
                table: "incidents",
                columns: new[] { "organization_id", "driver_id" });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_organization_id_severity_status",
                table: "incidents",
                columns: new[] { "organization_id", "severity", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_organization_id_status_occurred_at",
                table: "incidents",
                columns: new[] { "organization_id", "status", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_organization_id_vehicle_id_occurred_at",
                table: "incidents",
                columns: new[] { "organization_id", "vehicle_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incidents");
        }
    }
}
