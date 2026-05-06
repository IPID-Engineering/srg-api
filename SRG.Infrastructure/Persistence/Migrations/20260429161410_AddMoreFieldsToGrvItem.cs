using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreFieldsToGrvItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LineNumber",
                table: "GoodsReceivedVoucherItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "GoodsReceivedVoucherItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorPartNumber",
                table: "GoodsReceivedVoucherItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineNumber",
                table: "GoodsReceivedVoucherItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "GoodsReceivedVoucherItems");

            migrationBuilder.DropColumn(
                name: "VendorPartNumber",
                table: "GoodsReceivedVoucherItems");
        }
    }
}
