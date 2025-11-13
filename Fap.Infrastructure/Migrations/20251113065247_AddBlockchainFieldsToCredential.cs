using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockchainFieldsToCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BlockchainStoredAt",
                table: "Credentials",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockchainTransactionHash",
                table: "Credentials",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnBlockchain",
                table: "Credentials",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockchainStoredAt",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "BlockchainTransactionHash",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "IsOnBlockchain",
                table: "Credentials");
        }
    }
}
