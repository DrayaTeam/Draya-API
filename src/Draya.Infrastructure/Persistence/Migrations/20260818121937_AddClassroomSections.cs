using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassroomSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "LearningMaterials",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "SectionId",
                table: "LearningMaterials",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ClassroomSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassroomSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassroomSections_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterials_SectionId",
                table: "LearningMaterials",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassroomSections_ClassroomId",
                table: "ClassroomSections",
                column: "ClassroomId");

            // --- DATA SEEDING ---
            migrationBuilder.Sql(@"
                -- 0. Delete orphaned LearningMaterials (where ClassroomId doesn't exist in Classrooms table)
                -- This prevents FK conflicts later
                DELETE FROM [LearningMaterials]
                WHERE NOT EXISTS (SELECT 1 FROM [Classrooms] c WHERE c.[Id] = [LearningMaterials].[ClassroomId])
            ");

            migrationBuilder.Sql(@"
                -- 1. Create a General section for each existing Classroom
                INSERT INTO [ClassroomSections] ([Id], [ClassroomId], [Title], [Description], [Order], [CreatedAt])
                SELECT NEWID(), [Id], 'General', 'General section for unassigned materials', 0, GETUTCDATE()
                FROM [Classrooms]
            ");

            migrationBuilder.Sql(@"
                -- 2. Update all existing LearningMaterials to be associated with their Classroom's General section
                UPDATE lm
                SET lm.[SectionId] = cs.[Id]
                FROM [LearningMaterials] lm
                INNER JOIN [ClassroomSections] cs ON lm.[ClassroomId] = cs.[ClassroomId]
                WHERE cs.[Title] = 'General'
            ");
            // --------------------

            migrationBuilder.AddForeignKey(
                name: "FK_LearningMaterials_ClassroomSections_SectionId",
                table: "LearningMaterials",
                column: "SectionId",
                principalTable: "ClassroomSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningMaterials_ClassroomSections_SectionId",
                table: "LearningMaterials");

            migrationBuilder.DropTable(
                name: "ClassroomSections");

            migrationBuilder.DropIndex(
                name: "IX_LearningMaterials_SectionId",
                table: "LearningMaterials");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "LearningMaterials");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "LearningMaterials",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);
        }
    }
}
