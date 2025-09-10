using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectZenith.Api.Write.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Developers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AppVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AppVersions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Apps",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Apps",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Developers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Apps");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Apps");
        }
    }
}
