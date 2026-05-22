using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAddedByToOrderedMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AddedById",
                table: "OrderedMaterials",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddedByRole",
                table: "OrderedMaterials",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderedMaterials_AddedById",
                table: "OrderedMaterials",
                column: "AddedById");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderedMaterials_Users_AddedById",
                table: "OrderedMaterials",
                column: "AddedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderedMaterials_Users_AddedById",
                table: "OrderedMaterials");

            migrationBuilder.DropIndex(
                name: "IX_OrderedMaterials_AddedById",
                table: "OrderedMaterials");

            migrationBuilder.DropColumn(
                name: "AddedById",
                table: "OrderedMaterials");

            migrationBuilder.DropColumn(
                name: "AddedByRole",
                table: "OrderedMaterials");
        }
    }
}
