using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherSpecialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Teachers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LearningMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaterialType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningMaterials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParseStatus = table.Column<int>(type: "int", nullable: false),
                    ParseErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialVersions_LearningMaterials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "LearningMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VideoDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoDetails_LearningMaterials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "LearningMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TextContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmbeddingId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialChunks_MaterialVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "MaterialVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialChunks_VersionId",
                table: "MaterialChunks",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVersions_MaterialId",
                table: "MaterialVersions",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoDetails_MaterialId",
                table: "VideoDetails",
                column: "MaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialChunks");

            migrationBuilder.DropTable(
                name: "VideoDetails");

            migrationBuilder.DropTable(
                name: "MaterialVersions");

            migrationBuilder.DropTable(
                name: "LearningMaterials");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Teachers");
        }
    }
}
