using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontractorCrewsAndForemanHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CrewId",
                table: "SubcontractorWorkers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubcontractorCrews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubcontractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentForemanId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorCrews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubcontractorCrews_SubcontractorWorkers_CurrentForemanId",
                        column: x => x.CurrentForemanId,
                        principalTable: "SubcontractorWorkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SubcontractorCrews_Users_SubcontractorId",
                        column: x => x.SubcontractorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractorForemanHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CrewId = table.Column<Guid>(type: "uuid", nullable: false),
                    ForemanId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorForemanHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubcontractorForemanHistory_SubcontractorCrews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "SubcontractorCrews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubcontractorForemanHistory_SubcontractorWorkers_ForemanId",
                        column: x => x.ForemanId,
                        principalTable: "SubcontractorWorkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorWorkers_CrewId",
                table: "SubcontractorWorkers",
                column: "CrewId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorCrews_CurrentForemanId",
                table: "SubcontractorCrews",
                column: "CurrentForemanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorCrews_SubcontractorId",
                table: "SubcontractorCrews",
                column: "SubcontractorId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorForemanHistory_CrewId_EndDate",
                table: "SubcontractorForemanHistory",
                columns: new[] { "CrewId", "EndDate" },
                unique: true,
                filter: "\"EndDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorForemanHistory_ForemanId",
                table: "SubcontractorForemanHistory",
                column: "ForemanId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubcontractorWorkers_SubcontractorCrews_CrewId",
                table: "SubcontractorWorkers",
                column: "CrewId",
                principalTable: "SubcontractorCrews",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubcontractorWorkers_SubcontractorCrews_CrewId",
                table: "SubcontractorWorkers");

            migrationBuilder.DropTable(
                name: "SubcontractorForemanHistory");

            migrationBuilder.DropTable(
                name: "SubcontractorCrews");

            migrationBuilder.DropIndex(
                name: "IX_SubcontractorWorkers_CrewId",
                table: "SubcontractorWorkers");

            migrationBuilder.DropColumn(
                name: "CrewId",
                table: "SubcontractorWorkers");
        }
    }
}
