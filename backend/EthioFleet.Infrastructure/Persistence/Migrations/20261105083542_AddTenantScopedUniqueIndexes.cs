using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EthioFleet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantScopedUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_organization_id",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_code",
                table: "organizations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_domain_users_organization_id_normalized_email",
                table: "domain_users",
                columns: new[] { "organization_id", "normalized_email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_domain_users_organization_id_normalized_user_name",
                table: "domain_users",
                columns: new[] { "organization_id", "normalized_user_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_domain_user_roles_organization_id_user_id_role_id",
                table: "domain_user_roles",
                columns: new[] { "organization_id", "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_domain_roles_normalized_name",
                table: "domain_roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_organization_id_normalized_user_name",
                table: "AspNetUsers",
                columns: new[] { "organization_id", "normalized_user_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_organizations_code",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_domain_users_organization_id_normalized_email",
                table: "domain_users");

            migrationBuilder.DropIndex(
                name: "ix_domain_users_organization_id_normalized_user_name",
                table: "domain_users");

            migrationBuilder.DropIndex(
                name: "ix_domain_user_roles_organization_id_user_id_role_id",
                table: "domain_user_roles");

            migrationBuilder.DropIndex(
                name: "ix_domain_roles_normalized_name",
                table: "domain_roles");

            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_organization_id_normalized_user_name",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_organization_id",
                table: "AspNetUsers",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name",
                unique: true);
        }
    }
}
