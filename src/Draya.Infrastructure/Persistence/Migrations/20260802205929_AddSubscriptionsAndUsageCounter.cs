using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionsAndUsageCounter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxStudents = table.Column<int>(type: "int", nullable: false),
                    MaxStorageMB = table.Column<int>(type: "int", nullable: false),
                    MonthlyExamQuota = table.Column<int>(type: "int", nullable: false),
                    PriceMonthly = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                    table.CheckConstraint("CK_SubscriptionPlan_MaxStorageMB", "[MaxStorageMB] > 0");
                    table.CheckConstraint("CK_SubscriptionPlan_MaxStudents", "[MaxStudents] > 0");
                    table.CheckConstraint("CK_SubscriptionPlan_MonthlyExamQuota", "[MonthlyExamQuota] > 0");
                    table.CheckConstraint("CK_SubscriptionPlan_PriceMonthly", "[PriceMonthly] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "UsageCounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrentStudentsCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ExamsGeneratedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    StorageUsedMB = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageCounters", x => x.Id);
                    table.CheckConstraint("CK_UsageCounter_CurrentStudentsCount", "[CurrentStudentsCount] >= 0");
                    table.CheckConstraint("CK_UsageCounter_ExamsGeneratedCount", "[ExamsGeneratedCount] >= 0");
                    table.CheckConstraint("CK_UsageCounter_StorageUsedMB", "[StorageUsedMB] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "TeacherSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubscriptions", x => x.Id);
                    table.CheckConstraint("CK_TeacherSubscription_Status", "[Status] IN ('Active', 'Expired', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_TeacherSubscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubscription_TeacherId_Status",
                table: "TeacherSubscriptions",
                columns: new[] { "TeacherId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubscriptions_PlanId",
                table: "TeacherSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "UQ_UsageCounter_TeacherId_PeriodMonth",
                table: "UsageCounters",
                columns: new[] { "TeacherId", "PeriodMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherSubscriptions");

            migrationBuilder.DropTable(
                name: "UsageCounters");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");
        }
    }
}
