using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontractorCrewToDailyReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubcontractorCrewId",
                table: "DailyReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_Date_SubcontractorCrewId",
                table: "DailyReports",
                columns: new[] { "Date", "SubcontractorCrewId" },
                unique: true,
                filter: "\"SubcontractorCrewId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReports_SubcontractorCrewId",
                table: "DailyReports",
                column: "SubcontractorCrewId");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyReports_SubcontractorCrews_SubcontractorCrewId",
                table: "DailyReports",
                column: "SubcontractorCrewId",
                principalTable: "SubcontractorCrews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyReports_SubcontractorCrews_SubcontractorCrewId",
                table: "DailyReports");

            migrationBuilder.DropIndex(
                name: "IX_DailyReports_Date_SubcontractorCrewId",
                table: "DailyReports");

            migrationBuilder.DropIndex(
                name: "IX_DailyReports_SubcontractorCrewId",
                table: "DailyReports");

            migrationBuilder.DropColumn(
                name: "SubcontractorCrewId",
                table: "DailyReports");
        }
    }
}
