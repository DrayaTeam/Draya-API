using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddYouTubeVideoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoDetails_MaterialId",
                table: "VideoDetails");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Teachers");

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

            migrationBuilder.AlterColumn<string>(
                name: "FileUrl",
                table: "MaterialVersions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_VideoDetails_MaterialId",
                table: "VideoDetails",
                column: "MaterialId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VideoDetails_MaterialId",
                table: "VideoDetails");

            migrationBuilder.DropColumn(
                name: "EmbedUrl",
                table: "VideoDetails");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "VideoDetails");

            migrationBuilder.DropColumn(
                name: "ProviderVideoId",
                table: "VideoDetails");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Teachers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Teachers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileUrl",
                table: "MaterialVersions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoDetails_MaterialId",
                table: "VideoDetails",
                column: "MaterialId");
        }
    }
}
