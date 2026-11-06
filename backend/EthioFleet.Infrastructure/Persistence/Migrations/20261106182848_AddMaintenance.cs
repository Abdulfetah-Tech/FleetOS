using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthioFleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "maintenance_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    performed_on = table.Column<DateOnly>(type: "date", nullable: false),
                    odometer = table.Column<decimal>(type: "numeric(12,1)", precision: 12, scale: 1, nullable: false),
                    labor_cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    parts_cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    service_provider = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("pk_maintenance_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_maintenance_records_vehicles_organization_id_vehicle_id",
                        columns: x => new { x.organization_id, x.vehicle_id },
                        principalTable: "vehicles",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    next_due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    next_due_mileage = table.Column<decimal>(type: "numeric(12,1)", precision: 12, scale: 1, nullable: true),
                    interval_days = table.Column<int>(type: "integer", nullable: true),
                    interval_mileage = table.Column<decimal>(type: "numeric(12,1)", precision: 12, scale: 1, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_maintenance_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_maintenance_schedules_vehicles_organization_id_vehicle_id",
                        columns: x => new { x.organization_id, x.vehicle_id },
                        principalTable: "vehicles",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_records_organization_id_vehicle_id_performed_on",
                table: "maintenance_records",
                columns: new[] { "organization_id", "vehicle_id", "performed_on" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_schedules_organization_id_status_next_due_date",
                table: "maintenance_schedules",
                columns: new[] { "organization_id", "status", "next_due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_schedules_organization_id_vehicle_id_next_due_m",
                table: "maintenance_schedules",
                columns: new[] { "organization_id", "vehicle_id", "next_due_mileage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maintenance_records");

            migrationBuilder.DropTable(
                name: "maintenance_schedules");
        }
    }
}
