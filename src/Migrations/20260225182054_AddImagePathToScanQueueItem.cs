using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThePantry.Migrations
{
    /// <inheritdoc />
    public partial class AddImagePathToScanQueueItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "Pantry"),
                    OnHandCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductSkus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sku = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    InventoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSkus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSkus_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScanQueueItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Upc = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RawData = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LinkedInventoryItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ProductDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanQueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanQueueItems_InventoryItems_LinkedInventoryItemId",
                        column: x => x.LinkedInventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UsageHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InventoryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityUsed = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageHistories_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Category",
                table: "InventoryItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Name",
                table: "InventoryItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSkus_InventoryItemId",
                table: "ProductSkus",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSkus_Sku",
                table: "ProductSkus",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScanQueueItems_LinkedInventoryItemId",
                table: "ScanQueueItems",
                column: "LinkedInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanQueueItems_Status",
                table: "ScanQueueItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ScanQueueItems_Upc",
                table: "ScanQueueItems",
                column: "Upc");

            migrationBuilder.CreateIndex(
                name: "IX_UsageHistories_InventoryItemId",
                table: "UsageHistories",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageHistories_Timestamp",
                table: "UsageHistories",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSkus");

            migrationBuilder.DropTable(
                name: "ScanQueueItems");

            migrationBuilder.DropTable(
                name: "UsageHistories");

            migrationBuilder.DropTable(
                name: "InventoryItems");
        }
    }
}
