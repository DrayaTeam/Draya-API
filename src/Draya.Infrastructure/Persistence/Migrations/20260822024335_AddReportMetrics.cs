using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageExamDurationMinutes",
                table: "PerformanceReports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ClassroomPercentile",
                table: "PerformanceReports",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CompletedLessons",
                table: "PerformanceReports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuestionsAsked",
                table: "PerformanceReports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuestionsReplied",
                table: "PerformanceReports",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageExamDurationMinutes",
                table: "PerformanceReports");

            migrationBuilder.DropColumn(
                name: "ClassroomPercentile",
                table: "PerformanceReports");

            migrationBuilder.DropColumn(
                name: "CompletedLessons",
                table: "PerformanceReports");

            migrationBuilder.DropColumn(
                name: "TotalQuestionsAsked",
                table: "PerformanceReports");

            migrationBuilder.DropColumn(
                name: "TotalQuestionsReplied",
                table: "PerformanceReports");
        }
    }
}
