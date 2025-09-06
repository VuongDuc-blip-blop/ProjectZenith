using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectZenith.Api.Write.Migrations
{
    /// <inheritdoc />
    public partial class MoveStatusToAppVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppScreenshot_Apps_AppId",
                table: "AppScreenshot");

            migrationBuilder.DropForeignKey(
                name: "FK_AppTag_Apps_AppId",
                table: "AppTag");

            migrationBuilder.DropForeignKey(
                name: "FK_AppTag_Tag_TagId",
                table: "AppTag");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Apps_Status",
                table: "Apps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tag",
                table: "Tag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppTag",
                table: "AppTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppScreenshot",
                table: "AppScreenshot");

            migrationBuilder.DropColumn(
                name: "StatusReason",
                table: "Apps");

            migrationBuilder.RenameTable(
                name: "Tag",
                newName: "Tags");

            migrationBuilder.RenameTable(
                name: "AppTag",
                newName: "AppTags");

            migrationBuilder.RenameTable(
                name: "AppScreenshot",
                newName: "AppScreenshots");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Apps",
                newName: "AppStatus");

            migrationBuilder.RenameIndex(
                name: "IX_Tag_Name",
                table: "Tags",
                newName: "IX_Tags_Name");

            migrationBuilder.RenameIndex(
                name: "IX_AppTag_TagId",
                table: "AppTags",
                newName: "IX_AppTags_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_AppScreenshot_AppId",
                table: "AppScreenshots",
                newName: "IX_AppScreenshots_AppId");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AppVersions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StatusReason",
                table: "AppVersions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AppScreenshots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tags",
                table: "Tags",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppTags",
                table: "AppTags",
                columns: new[] { "AppId", "TagId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppScreenshots",
                table: "AppScreenshots",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppVersions_Status",
                table: "AppVersions",
                sql: "[Status] IN ('Draft', 'ValidationFailed', 'PendingApproval', 'PendingValidation', 'Published', 'Rejected','Superseded', 'Banned')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Apps_AppStatus",
                table: "Apps",
                sql: "[AppStatus] IN ('Active', 'Delisted')");

            migrationBuilder.AddForeignKey(
                name: "FK_AppScreenshots_Apps_AppId",
                table: "AppScreenshots",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppTags_Apps_AppId",
                table: "AppTags",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppTags_Tags_TagId",
                table: "AppTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppScreenshots_Apps_AppId",
                table: "AppScreenshots");

            migrationBuilder.DropForeignKey(
                name: "FK_AppTags_Apps_AppId",
                table: "AppTags");

            migrationBuilder.DropForeignKey(
                name: "FK_AppTags_Tags_TagId",
                table: "AppTags");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppVersions_Status",
                table: "AppVersions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Apps_AppStatus",
                table: "Apps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tags",
                table: "Tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppTags",
                table: "AppTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppScreenshots",
                table: "AppScreenshots");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "StatusReason",
                table: "AppVersions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AppScreenshots");

            migrationBuilder.RenameTable(
                name: "Tags",
                newName: "Tag");

            migrationBuilder.RenameTable(
                name: "AppTags",
                newName: "AppTag");

            migrationBuilder.RenameTable(
                name: "AppScreenshots",
                newName: "AppScreenshot");

            migrationBuilder.RenameColumn(
                name: "AppStatus",
                table: "Apps",
                newName: "Status");

            migrationBuilder.RenameIndex(
                name: "IX_Tags_Name",
                table: "Tag",
                newName: "IX_Tag_Name");

            migrationBuilder.RenameIndex(
                name: "IX_AppTags_TagId",
                table: "AppTag",
                newName: "IX_AppTag_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_AppScreenshots_AppId",
                table: "AppScreenshot",
                newName: "IX_AppScreenshot_AppId");

            migrationBuilder.AddColumn<string>(
                name: "StatusReason",
                table: "Apps",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tag",
                table: "Tag",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppTag",
                table: "AppTag",
                columns: new[] { "AppId", "TagId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppScreenshot",
                table: "AppScreenshot",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Apps_Status",
                table: "Apps",
                sql: "[Status] IN ('Draft', 'ValidationFailed', 'PendingApproval', 'PendingValidation', 'Published', 'Rejected', 'Banned')");

            migrationBuilder.AddForeignKey(
                name: "FK_AppScreenshot_Apps_AppId",
                table: "AppScreenshot",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppTag_Apps_AppId",
                table: "AppTag",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppTag_Tag_TagId",
                table: "AppTag",
                column: "TagId",
                principalTable: "Tag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
