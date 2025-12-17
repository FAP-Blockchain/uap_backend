using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockchainAuditFieldsToActionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BlockNumber",
                table: "ActionLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventName",
                table: "ActionLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionHash",
                table: "ActionLogs",
                type: "nvarchar(66)",
                maxLength: 66,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_BlockNumber",
                table: "ActionLogs",
                column: "BlockNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_TransactionHash",
                table: "ActionLogs",
                column: "TransactionHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActionLogs_BlockNumber",
                table: "ActionLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActionLogs_TransactionHash",
                table: "ActionLogs");

            migrationBuilder.DropColumn(
                name: "BlockNumber",
                table: "ActionLogs");

            migrationBuilder.DropColumn(
                name: "EventName",
                table: "ActionLogs");

            migrationBuilder.DropColumn(
                name: "TransactionHash",
                table: "ActionLogs");
        }
    }
}
