using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSubscriptionsToWalletAndCommissionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherSubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UsageCounter_CurrentStudentsCount",
                table: "UsageCounters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UsageCounter_ExamsGeneratedCount",
                table: "UsageCounters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UsageCounter_StorageUsedMB",
                table: "UsageCounters");

            migrationBuilder.DropColumn(
                name: "StorageUsedMB",
                table: "UsageCounters");

            migrationBuilder.RenameColumn(
                name: "ExamsGeneratedCount",
                table: "UsageCounters",
                newName: "PaidExamsGenerated");

            migrationBuilder.RenameColumn(
                name: "CurrentStudentsCount",
                table: "UsageCounters",
                newName: "FreeExamsUsed");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UsageCounters",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UsageCounters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TeacherAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.CheckConstraint("CK_PaymentTransaction_GrossAmount", "[GrossAmount] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "PlatformSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AIExamPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 20.00m),
                    FreeMonthlyAIExamQuota = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    PlatformCommissionPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 5.00m),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSettings", x => x.Id);
                    table.CheckConstraint("CK_PlatformSetting_AIExamPrice", "[AIExamPrice] >= 0");
                    table.CheckConstraint("CK_PlatformSetting_FreeMonthlyAIExamQuota", "[FreeMonthlyAIExamQuota] >= 0");
                    table.CheckConstraint("CK_PlatformSetting_PlatformCommissionPercent", "[PlatformCommissionPercent] >= 0 AND [PlatformCommissionPercent] <= 100");
                });

            migrationBuilder.CreateTable(
                name: "TeacherPayoutAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountIdentifier = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherPayoutAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeacherWallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EarnedBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    PurchasedBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherWallets", x => x.Id);
                    table.CheckConstraint("CK_TeacherWallet_EarnedBalance", "[EarnedBalance] >= 0");
                    table.CheckConstraint("CK_TeacherWallet_PurchasedBalance", "[PurchasedBalance] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalRequests", x => x.Id);
                    table.CheckConstraint("CK_WithdrawalRequest_Amount", "[Amount] > 0");
                });

            migrationBuilder.InsertData(
                table: "PlatformSettings",
                columns: new[] { "Id", "AIExamPrice", "FreeMonthlyAIExamQuota", "PlatformCommissionPercent", "UpdatedAt", "UpdatedByAdminId" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), 20.00m, 3, 5.00m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsageCounter_FreeExamsUsed",
                table: "UsageCounters",
                sql: "[FreeExamsUsed] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsageCounter_PaidExamsGenerated",
                table: "UsageCounters",
                sql: "[PaidExamsGenerated] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_ClassroomId",
                table: "PaymentTransactions",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_PayerId",
                table: "PaymentTransactions",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherPayoutAccount_TeacherId",
                table: "TeacherPayoutAccounts",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "UQ_TeacherWallet_TeacherId",
                table: "TeacherWallets",
                column: "TeacherId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_CreatedAt",
                table: "WalletTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_TeacherId",
                table: "WalletTransactions",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequest_Status",
                table: "WithdrawalRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequest_TeacherId",
                table: "WithdrawalRequests",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "PlatformSettings");

            migrationBuilder.DropTable(
                name: "TeacherPayoutAccounts");

            migrationBuilder.DropTable(
                name: "TeacherWallets");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "WithdrawalRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UsageCounter_FreeExamsUsed",
                table: "UsageCounters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UsageCounter_PaidExamsGenerated",
                table: "UsageCounters");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UsageCounters");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UsageCounters");

            migrationBuilder.RenameColumn(
                name: "PaidExamsGenerated",
                table: "UsageCounters",
                newName: "ExamsGeneratedCount");

            migrationBuilder.RenameColumn(
                name: "FreeExamsUsed",
                table: "UsageCounters",
                newName: "CurrentStudentsCount");

            migrationBuilder.AddColumn<decimal>(
                name: "StorageUsedMB",
                table: "UsageCounters",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MaxStorageMB = table.Column<int>(type: "int", nullable: false),
                    MaxStudents = table.Column<int>(type: "int", nullable: false),
                    MonthlyExamQuota = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PriceMonthly = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m)
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
                name: "TeacherSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "CreatedAt", "IsActive", "MaxStorageMB", "MaxStudents", "MonthlyExamQuota", "Name" },
                values: new object[] { new Guid("b1e9f1d2-4c3a-4e5b-9f6a-8d7e6c5b4a3f"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 500, 30, 3, "Free" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsageCounter_CurrentStudentsCount",
                table: "UsageCounters",
                sql: "[CurrentStudentsCount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsageCounter_ExamsGeneratedCount",
                table: "UsageCounters",
                sql: "[ExamsGeneratedCount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsageCounter_StorageUsedMB",
                table: "UsageCounters",
                sql: "[StorageUsedMB] >= 0");

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
        }
    }
}
