using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontractorCrewToWorkOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubcontractorCrewId",
                table: "WorkOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_SubcontractorCrewId",
                table: "WorkOrders",
                column: "SubcontractorCrewId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkOrders_SubcontractorCrews_SubcontractorCrewId",
                table: "WorkOrders",
                column: "SubcontractorCrewId",
                principalTable: "SubcontractorCrews",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkOrders_SubcontractorCrews_SubcontractorCrewId",
                table: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_WorkOrders_SubcontractorCrewId",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "SubcontractorCrewId",
                table: "WorkOrders");
        }
    }
}
