using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassroomTypeGradeLevelAndFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var defaultTypeId = new Guid("11111111-1111-1111-1111-111111111111");
            var defaultGradeId = new Guid("22222222-2222-2222-2222-222222222222");

            migrationBuilder.CreateTable(
                name: "ClassroomTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeLevels", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ClassroomTypes",
                columns: new[] { "Id", "Name", "Description", "IsActive", "CreatedAt" },
                values: new object[] { defaultTypeId, "Standard Group", "Default classroom type for existing classrooms", true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            migrationBuilder.InsertData(
                table: "GradeLevels",
                columns: new[] { "Id", "Name", "Description", "SortOrder", "IsActive", "CreatedAt" },
                values: new object[] { defaultGradeId, "General Level", "Default grade level for existing classrooms", 1, true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            migrationBuilder.AddColumn<Guid>(
                name: "ClassroomTypeId",
                table: "Classrooms",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: defaultTypeId);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Classrooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2099, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<Guid>(
                name: "GradeLevelId",
                table: "Classrooms",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: defaultGradeId);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Classrooms",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Classrooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_ClassroomTypeId",
                table: "Classrooms",
                column: "ClassroomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Classrooms_GradeLevelId",
                table: "Classrooms",
                column: "GradeLevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classrooms_ClassroomTypes_ClassroomTypeId",
                table: "Classrooms",
                column: "ClassroomTypeId",
                principalTable: "ClassroomTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Classrooms_GradeLevels_GradeLevelId",
                table: "Classrooms",
                column: "GradeLevelId",
                principalTable: "GradeLevels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classrooms_ClassroomTypes_ClassroomTypeId",
                table: "Classrooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Classrooms_GradeLevels_GradeLevelId",
                table: "Classrooms");

            migrationBuilder.DropTable(
                name: "ClassroomTypes");

            migrationBuilder.DropTable(
                name: "GradeLevels");

            migrationBuilder.DropIndex(
                name: "IX_Classrooms_ClassroomTypeId",
                table: "Classrooms");

            migrationBuilder.DropIndex(
                name: "IX_Classrooms_GradeLevelId",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "ClassroomTypeId",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "GradeLevelId",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Classrooms");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Classrooms");
        }
    }
}
