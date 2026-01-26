using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicineLog.Migrations
{
    /// <inheritdoc />
    public partial class Photos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_content_type",
                schema: "public",
                table: "medicine_log_entry_items",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "photo_length",
                schema: "public",
                table: "medicine_log_entry_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "photo_path",
                schema: "public",
                table: "medicine_log_entry_items",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "photo_content_type",
                schema: "public",
                table: "medicine_log_entry_items");

            migrationBuilder.DropColumn(
                name: "photo_length",
                schema: "public",
                table: "medicine_log_entry_items");

            migrationBuilder.DropColumn(
                name: "photo_path",
                schema: "public",
                table: "medicine_log_entry_items");
        }
    }
}
