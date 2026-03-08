using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThePantry.Migrations
{
    /// <inheritdoc />
    public partial class AddShelfLifeAndOpenedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOpened",
                table: "InventoryItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenedDate",
                table: "InventoryItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShelfLifeDays",
                table: "InventoryItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "UseWithinDays",
                table: "InventoryItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 7);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOpened",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "OpenedDate",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ShelfLifeDays",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "UseWithinDays",
                table: "InventoryItems");
        }
    }
}
