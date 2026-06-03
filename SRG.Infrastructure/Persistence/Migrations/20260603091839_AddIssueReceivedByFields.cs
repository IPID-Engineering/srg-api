using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueReceivedByFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Issues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedByName",
                table: "Issues",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceivedBySubcontractorWorkerId",
                table: "Issues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceivedByWorkerId",
                table: "Issues",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ReceivedBySubcontractorWorkerId",
                table: "Issues",
                column: "ReceivedBySubcontractorWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ReceivedByWorkerId",
                table: "Issues",
                column: "ReceivedByWorkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_SubcontractorWorkers_ReceivedBySubcontractorWorkerId",
                table: "Issues",
                column: "ReceivedBySubcontractorWorkerId",
                principalTable: "SubcontractorWorkers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Workers_ReceivedByWorkerId",
                table: "Issues",
                column: "ReceivedByWorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issues_SubcontractorWorkers_ReceivedBySubcontractorWorkerId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Workers_ReceivedByWorkerId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ReceivedBySubcontractorWorkerId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ReceivedByWorkerId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ReceivedByName",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ReceivedBySubcontractorWorkerId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ReceivedByWorkerId",
                table: "Issues");
        }
    }
}
