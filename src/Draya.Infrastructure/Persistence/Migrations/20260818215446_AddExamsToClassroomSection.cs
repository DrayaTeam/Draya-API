using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExamsToClassroomSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Exams_SectionId",
                table: "Exams",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_ClassroomSections_SectionId",
                table: "Exams",
                column: "SectionId",
                principalTable: "ClassroomSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exams_ClassroomSections_SectionId",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_SectionId",
                table: "Exams");
        }
    }
}
