using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRateGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RateGroupId",
                table: "SubcontractorWorkers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RateGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    HourlyCost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SubcontractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RateGroups_Users_SubcontractorId",
                        column: x => x.SubcontractorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorWorkers_RateGroupId",
                table: "SubcontractorWorkers",
                column: "RateGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RateGroups_SubcontractorId",
                table: "RateGroups",
                column: "SubcontractorId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubcontractorWorkers_RateGroups_RateGroupId",
                table: "SubcontractorWorkers",
                column: "RateGroupId",
                principalTable: "RateGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubcontractorWorkers_RateGroups_RateGroupId",
                table: "SubcontractorWorkers");

            migrationBuilder.DropTable(
                name: "RateGroups");

            migrationBuilder.DropIndex(
                name: "IX_SubcontractorWorkers_RateGroupId",
                table: "SubcontractorWorkers");

            migrationBuilder.DropColumn(
                name: "RateGroupId",
                table: "SubcontractorWorkers");
        }
    }
}
