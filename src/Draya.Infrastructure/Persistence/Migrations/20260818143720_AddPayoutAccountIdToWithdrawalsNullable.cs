using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Draya.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutAccountIdToWithdrawalsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PayoutAccountId",
                table: "WithdrawalRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_PayoutAccountId",
                table: "WithdrawalRequests",
                column: "PayoutAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequests_TeacherPayoutAccounts_PayoutAccountId",
                table: "WithdrawalRequests",
                column: "PayoutAccountId",
                principalTable: "TeacherPayoutAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequests_TeacherPayoutAccounts_PayoutAccountId",
                table: "WithdrawalRequests");

            migrationBuilder.DropIndex(
                name: "IX_WithdrawalRequests_PayoutAccountId",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "PayoutAccountId",
                table: "WithdrawalRequests");
        }
    }
}
