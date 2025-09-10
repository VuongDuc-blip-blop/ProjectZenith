using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectZenith.Api.Write.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeAccountIdToDeveloper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeAccountId",
                table: "Developers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeAccountId",
                table: "Developers");
        }
    }
}
