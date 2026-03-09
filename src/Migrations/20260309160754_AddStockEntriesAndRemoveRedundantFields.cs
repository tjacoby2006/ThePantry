using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThePantry.Migrations
{
    /// <inheritdoc />
    public partial class AddStockEntriesAndRemoveRedundantFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;", suppressTransaction: true);

            migrationBuilder.CreateTable(
                name: "StockEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InventoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsOpened = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpenedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockEntries_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockEntries_InventoryItemId",
                table: "StockEntries",
                column: "InventoryItemId");

            migrationBuilder.Sql(@"
                INSERT INTO StockEntries (InventoryItemId, AddedDate, IsOpened, OpenedDate)
                SELECT i.Id, i.CreatedDate, 0, NULL
                FROM InventoryItems i
                WHERE i.OnHandCount = 1;
            ");
            // For items with OnHandCount > 1, we need to add the remaining entries
            // SQLite doesn't have a built-in tally table, so we use a recursive CTE to generate rows
            migrationBuilder.Sql(@"
                INSERT INTO StockEntries (InventoryItemId, AddedDate, IsOpened, OpenedDate)
                WITH RECURSIVE cnt(x) AS (
                    SELECT 1
                    UNION ALL
                    SELECT x + 1 FROM cnt WHERE x < 100000 -- Assuming max 100 items per record for migration
                )
                SELECT i.Id, i.CreatedDate, 0, NULL
                FROM InventoryItems i
                JOIN cnt ON cnt.x < i.OnHandCount
                WHERE i.OnHandCount > 1;
            ");

            migrationBuilder.DropColumn(
                name: "IsOpened",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "OnHandCount",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "OpenedDate",
                table: "InventoryItems");

            migrationBuilder.Sql("PRAGMA foreign_keys = ON;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;", suppressTransaction: true);

            migrationBuilder.DropTable(
                name: "StockEntries");

            migrationBuilder.AddColumn<bool>(
                name: "IsOpened",
                table: "InventoryItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OnHandCount",
                table: "InventoryItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedDate",
                table: "InventoryItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("PRAGMA foreign_keys = ON;", suppressTransaction: true);
        }
    }
}
