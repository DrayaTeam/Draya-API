using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExamGenerationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamGenerations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedCount = table.Column<int>(type: "int", nullable: false),
                    GeneratedCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamGenerations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamGenerations_IdempotencyKey",
                table: "ExamGenerations",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamGenerations");
        }
    }
}
