using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExpenseManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWallets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    InitialBalance = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Wallets",
                columns: new[] { "Id", "Color", "InitialBalance", "IsArchived", "IsDefault", "Name", "Type" },
                values: new object[,]
                {
                    { 1, "#22C55E", 0m, false, true, "Cash", 0 },
                    { 2, "#3B82F6", 0m, false, false, "Bank Account", 1 },
                    { 3, "#A855F7", 0m, false, false, "Credit Card", 2 },
                    { 4, "#F97316", 0m, false, false, "Mobile Banking", 3 }
                });

            // Default existing transactions (from before wallets existed) to the seeded "Cash"
            // wallet (Id = 1) rather than an orphaned WalletId = 0.
            migrationBuilder.AddColumn<int>(
                name: "WalletId",
                table: "Transactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_WalletId",
                table: "Transactions",
                column: "WalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_WalletId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "WalletId",
                table: "Transactions");
        }
    }
}
