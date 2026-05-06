using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentSubcontractorWorkerSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "author_id",
                table: "daily_report_comments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "author_email",
                table: "daily_report_comments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "author_role",
                table: "daily_report_comments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "subcontractor_worker_id",
                table: "daily_report_comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_report_comments_subcontractor_worker_id",
                table: "daily_report_comments",
                column: "subcontractor_worker_id");

            migrationBuilder.AddForeignKey(
                name: "FK_daily_report_comments_SubcontractorWorkers_subcontractor_wo~",
                table: "daily_report_comments",
                column: "subcontractor_worker_id",
                principalTable: "SubcontractorWorkers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_daily_report_comments_SubcontractorWorkers_subcontractor_wo~",
                table: "daily_report_comments");

            migrationBuilder.DropIndex(
                name: "IX_daily_report_comments_subcontractor_worker_id",
                table: "daily_report_comments");

            migrationBuilder.DropColumn(
                name: "author_email",
                table: "daily_report_comments");

            migrationBuilder.DropColumn(
                name: "author_role",
                table: "daily_report_comments");

            migrationBuilder.DropColumn(
                name: "subcontractor_worker_id",
                table: "daily_report_comments");

            migrationBuilder.AlterColumn<Guid>(
                name: "author_id",
                table: "daily_report_comments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
