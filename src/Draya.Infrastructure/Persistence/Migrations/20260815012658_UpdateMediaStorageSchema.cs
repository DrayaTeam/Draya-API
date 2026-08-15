using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMediaStorageSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbedUrl",
                table: "VideoDetails");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "VideoDetails");

            migrationBuilder.DropColumn(
                name: "ProviderVideoId",
                table: "VideoDetails");

            migrationBuilder.RenameColumn(
                name: "FileUrl",
                table: "MaterialVersions",
                newName: "ResourceType");

            migrationBuilder.AddColumn<string>(
                name: "Format",
                table: "MaterialVersions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "MaterialVersions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderAssetId",
                table: "MaterialVersions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Format",
                table: "MaterialVersions");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "MaterialVersions");

            migrationBuilder.DropColumn(
                name: "ProviderAssetId",
                table: "MaterialVersions");

            migrationBuilder.RenameColumn(
                name: "ResourceType",
                table: "MaterialVersions",
                newName: "FileUrl");

            migrationBuilder.AddColumn<string>(
                name: "EmbedUrl",
                table: "VideoDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "VideoDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderVideoId",
                table: "VideoDetails",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
