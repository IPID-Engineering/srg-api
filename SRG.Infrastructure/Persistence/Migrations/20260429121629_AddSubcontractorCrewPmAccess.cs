using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontractorCrewPmAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubcontractorCrewPmAccessList",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CrewId = table.Column<Guid>(type: "uuid", nullable: false),
                    PmUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedBySubcontractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorCrewPmAccessList", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubcontractorCrewPmAccessList_SubcontractorCrews_CrewId",
                        column: x => x.CrewId,
                        principalTable: "SubcontractorCrews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubcontractorCrewPmAccessList_Users_GrantedBySubcontractorId",
                        column: x => x.GrantedBySubcontractorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubcontractorCrewPmAccessList_Users_PmUserId",
                        column: x => x.PmUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorCrewPmAccessList_CrewId_PmUserId",
                table: "SubcontractorCrewPmAccessList",
                columns: new[] { "CrewId", "PmUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorCrewPmAccessList_GrantedBySubcontractorId",
                table: "SubcontractorCrewPmAccessList",
                column: "GrantedBySubcontractorId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorCrewPmAccessList_PmUserId",
                table: "SubcontractorCrewPmAccessList",
                column: "PmUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubcontractorCrewPmAccessList");
        }
    }
}
