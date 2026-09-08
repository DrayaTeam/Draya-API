using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeaknessAndHistoryModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFinalized",
                table: "AnswerGradingResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "AnswerGradingResults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByTeacherId",
                table: "AnswerGradingResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentWeaknesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TopicNameSnapshot = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CurrentProficiencyPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentWeaknesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentWeaknesses_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentWeaknessHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentWeaknessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousProficiencyPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    NewProficiencyPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PreviousIsActive = table.Column<bool>(type: "bit", nullable: false),
                    NewIsActive = table.Column<bool>(type: "bit", nullable: false),
                    SourceAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentWeaknessHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentWeaknessHistories_StudentExamAttempts_SourceAttemptId",
                        column: x => x.SourceAttemptId,
                        principalTable: "StudentExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentWeaknessHistories_StudentWeaknesses_StudentWeaknessId",
                        column: x => x.StudentWeaknessId,
                        principalTable: "StudentWeaknesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeaknessReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentWeaknessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProficiencyAtGeneration = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AiExplanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeyConcepts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommonMistakes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recommendations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsOutdated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaknessReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaknessReviews_StudentWeaknesses_StudentWeaknessId",
                        column: x => x.StudentWeaknessId,
                        principalTable: "StudentWeaknesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentWeaknesses_StudentId_TopicId",
                table: "StudentWeaknesses",
                columns: new[] { "StudentId", "TopicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentWeaknessHistories_SourceAttemptId",
                table: "StudentWeaknessHistories",
                column: "SourceAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentWeaknessHistories_StudentWeaknessId",
                table: "StudentWeaknessHistories",
                column: "StudentWeaknessId");

            migrationBuilder.CreateIndex(
                name: "IX_WeaknessReviews_StudentWeaknessId",
                table: "WeaknessReviews",
                column: "StudentWeaknessId",
                unique: true,
                filter: "[IsOutdated] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentWeaknessHistories");

            migrationBuilder.DropTable(
                name: "WeaknessReviews");

            migrationBuilder.DropTable(
                name: "StudentWeaknesses");

            migrationBuilder.DropColumn(
                name: "IsFinalized",
                table: "AnswerGradingResults");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "AnswerGradingResults");

            migrationBuilder.DropColumn(
                name: "ReviewedByTeacherId",
                table: "AnswerGradingResults");
        }
    }
}
