using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWarehouseUserFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Users_OwnerId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_OwnerId",
                table: "Warehouses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_OwnerId",
                table: "Warehouses",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Users_OwnerId",
                table: "Warehouses",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
