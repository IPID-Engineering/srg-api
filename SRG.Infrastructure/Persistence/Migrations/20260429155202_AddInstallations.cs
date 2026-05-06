using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InstallationId",
                table: "OrderedWorks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Installations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Installations_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderedWorks_InstallationId",
                table: "OrderedWorks",
                column: "InstallationId");

            migrationBuilder.CreateIndex(
                name: "IX_Installations_SectionId",
                table: "Installations",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderedWorks_Installations_InstallationId",
                table: "OrderedWorks",
                column: "InstallationId",
                principalTable: "Installations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderedWorks_Installations_InstallationId",
                table: "OrderedWorks");

            migrationBuilder.DropTable(
                name: "Installations");

            migrationBuilder.DropIndex(
                name: "IX_OrderedWorks_InstallationId",
                table: "OrderedWorks");

            migrationBuilder.DropColumn(
                name: "InstallationId",
                table: "OrderedWorks");
        }
    }
}
