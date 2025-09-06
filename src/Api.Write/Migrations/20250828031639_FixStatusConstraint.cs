using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectZenith.Api.Write.Migrations
{
    /// <inheritdoc />
    public partial class FixStatusConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Apps_Status",
                table: "Apps");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Apps_Status",
                table: "Apps",
                sql: "[Status] IN ('Draft', 'ValidationFailed', 'PendingApproval', 'PendingValidation', 'Published', 'Rejected', 'Banned')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Apps_Status",
                table: "Apps");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Apps_Status",
                table: "Apps",
                sql: "[Status] IN ('Draft', 'Pending', 'Published', 'Rejected', 'Banned')");
        }
    }
}
