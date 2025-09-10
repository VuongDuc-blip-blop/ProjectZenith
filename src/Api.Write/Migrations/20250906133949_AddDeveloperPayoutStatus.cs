using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectZenith.Api.Write.Migrations
{
    /// <inheritdoc />
    public partial class AddDeveloperPayoutStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PayoutStatus",
                table: "Developers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayoutStatus",
                table: "Developers");
        }
    }
}
