using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedFreePlanAndBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "CreatedAt", "IsActive", "MaxStorageMB", "MaxStudents", "MonthlyExamQuota", "Name" },
                values: new object[] { new Guid("b1e9f1d2-4c3a-4e5b-9f6a-8d7e6c5b4a3f"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 500, 30, 3, "Free" });

            // Backfill existing teachers who don't have an active subscription with the Free plan
            migrationBuilder.Sql(@"INSERT INTO TeacherSubscriptions (Id, TeacherId, PlanId, StartDate, EndDate, Status, CreatedAt)
SELECT NEWID(), t.UserId, 'b1e9f1d2-4c3a-4e5b-9f6a-8d7e6c5b4a3f', SYSUTCDATETIME(), NULL, 'Active', SYSUTCDATETIME()
FROM Teachers t
WHERE NOT EXISTS (
    SELECT 1 FROM TeacherSubscriptions ts WHERE ts.TeacherId = t.UserId AND ts.Status = 'Active'
);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seeded Free plan
            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b1e9f1d2-4c3a-4e5b-9f6a-8d7e6c5b4a3f"));

            // Optionally remove backfilled teacher subscriptions created by this migration
            migrationBuilder.Sql(@"DELETE FROM TeacherSubscriptions WHERE PlanId = 'b1e9f1d2-4c3a-4e5b-9f6a-8d7e6c5b4a3f' AND CreatedAt >= '2024-01-01'");
        }
    }
}
