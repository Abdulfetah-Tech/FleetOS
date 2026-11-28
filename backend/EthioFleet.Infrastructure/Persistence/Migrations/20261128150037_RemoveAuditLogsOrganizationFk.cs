using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthioFleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAuditLogsOrganizationFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DO $$
BEGIN
    ALTER TABLE audit_logs DROP CONSTRAINT fk_audit_logs_organizations_organization_id;
EXCEPTION
    WHEN others THEN NULL;
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DO $$
BEGIN
    ALTER TABLE audit_logs ADD CONSTRAINT fk_audit_logs_organizations_organization_id
        FOREIGN KEY (organization_id) REFERENCES organizations(id) ON DELETE RESTRICT;
EXCEPTION
    WHEN others THEN NULL;
END $$;");
        }
    }
}