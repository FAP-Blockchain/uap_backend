using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTxFromTxToContractAddressToActionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContractAddress",
                table: "ActionLogs",
                type: "nvarchar(42)",
                maxLength: 42,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TxFrom",
                table: "ActionLogs",
                type: "nvarchar(42)",
                maxLength: 42,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TxTo",
                table: "ActionLogs",
                type: "nvarchar(42)",
                maxLength: 42,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_ContractAddress",
                table: "ActionLogs",
                column: "ContractAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_TxFrom",
                table: "ActionLogs",
                column: "TxFrom");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_TxTo",
                table: "ActionLogs",
                column: "TxTo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActionLogs_ContractAddress",
                table: "ActionLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActionLogs_TxFrom",
                table: "ActionLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActionLogs_TxTo",
                table: "ActionLogs");

            migrationBuilder.DropColumn(
                name: "ContractAddress",
                table: "ActionLogs");

            migrationBuilder.DropColumn(
                name: "TxFrom",
                table: "ActionLogs");

            migrationBuilder.DropColumn(
                name: "TxTo",
                table: "ActionLogs");
        }
    }
}
