using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthioFleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFuelRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "ak_trips_organization_id_id",
                table: "trips",
                columns: new[] { "organization_id", "id" });

            migrationBuilder.CreateTable(
                name: "fuel_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    quantity_liters = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    odometer = table.Column<decimal>(type: "numeric(12,1)", precision: 12, scale: 1, nullable: false),
                    station = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    receipt_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("pk_fuel_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_fuel_records_drivers_organization_id_driver_id",
                        columns: x => new { x.organization_id, x.driver_id },
                        principalTable: "drivers",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fuel_records_trips_organization_id_trip_id",
                        columns: x => new { x.organization_id, x.trip_id },
                        principalTable: "trips",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fuel_records_vehicles_organization_id_vehicle_id",
                        columns: x => new { x.organization_id, x.vehicle_id },
                        principalTable: "vehicles",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fuel_records_organization_id_driver_id_occurred_at",
                table: "fuel_records",
                columns: new[] { "organization_id", "driver_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fuel_records_organization_id_trip_id",
                table: "fuel_records",
                columns: new[] { "organization_id", "trip_id" });

            migrationBuilder.CreateIndex(
                name: "ix_fuel_records_organization_id_vehicle_id_occurred_at",
                table: "fuel_records",
                columns: new[] { "organization_id", "vehicle_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fuel_records");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_trips_organization_id_id",
                table: "trips");
        }
    }
}
