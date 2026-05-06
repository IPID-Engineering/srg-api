using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionToOrderedWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SectionId",
                table: "OrderedWorks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderedWorks_SectionId",
                table: "OrderedWorks",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderedWorks_Sections_SectionId",
                table: "OrderedWorks",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderedWorks_Sections_SectionId",
                table: "OrderedWorks");

            migrationBuilder.DropIndex(
                name: "IX_OrderedWorks_SectionId",
                table: "OrderedWorks");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "OrderedWorks");
        }
    }
}
