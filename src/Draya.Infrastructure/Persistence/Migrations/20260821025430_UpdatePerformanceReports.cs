using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePerformanceReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentStreak",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityDate",
                table: "Students",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStreak",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "LastActivityDate",
                table: "Students");
        }
    }
}
