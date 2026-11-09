using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthioFleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleInspections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspector_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inspected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    odometer = table.Column<decimal>(type: "numeric(12,1)", precision: 12, scale: 1, nullable: false),
                    checklist_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    checklist_items_json = table.Column<string>(type: "jsonb", nullable: false),
                    items_json = table.Column<string>(type: "jsonb", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    requires_vehicle_out_of_service = table.Column<bool>(type: "boolean", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_vehicle_inspections", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicle_inspections_vehicles_organization_id_vehicle_id",
                        columns: x => new { x.organization_id, x.vehicle_id },
                        principalTable: "vehicles",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_inspections_organization_id_status_inspected_at",
                table: "vehicle_inspections",
                columns: new[] { "organization_id", "status", "inspected_at" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_inspections_organization_id_vehicle_id_inspected_at",
                table: "vehicle_inspections",
                columns: new[] { "organization_id", "vehicle_id", "inspected_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicle_inspections");
        }
    }
}
