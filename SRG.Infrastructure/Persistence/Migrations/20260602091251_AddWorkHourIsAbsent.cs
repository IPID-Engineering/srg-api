using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkHourIsAbsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAbsent",
                table: "WorkHours",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByWorkerId",
                table: "MaterialRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_CreatedByWorkerId",
                table: "MaterialRequests",
                column: "CreatedByWorkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialRequests_SubcontractorWorkers_CreatedByWorkerId",
                table: "MaterialRequests",
                column: "CreatedByWorkerId",
                principalTable: "SubcontractorWorkers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialRequests_SubcontractorWorkers_CreatedByWorkerId",
                table: "MaterialRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaterialRequests_CreatedByWorkerId",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "IsAbsent",
                table: "WorkHours");

            migrationBuilder.DropColumn(
                name: "CreatedByWorkerId",
                table: "MaterialRequests");
        }
    }
}
