using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddForemanSupportToStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_daily_report_status_history_Users_changed_by_id",
                table: "daily_report_status_history");

            migrationBuilder.AlterColumn<Guid>(
                name: "changed_by_id",
                table: "daily_report_status_history",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "changed_by_email",
                table: "daily_report_status_history",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "changed_by_worker_id",
                table: "daily_report_status_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_report_status_history_changed_by_worker_id",
                table: "daily_report_status_history",
                column: "changed_by_worker_id");

            migrationBuilder.AddForeignKey(
                name: "FK_daily_report_status_history_SubcontractorWorkers_changed_by~",
                table: "daily_report_status_history",
                column: "changed_by_worker_id",
                principalTable: "SubcontractorWorkers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_daily_report_status_history_Users_changed_by_id",
                table: "daily_report_status_history",
                column: "changed_by_id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_daily_report_status_history_SubcontractorWorkers_changed_by~",
                table: "daily_report_status_history");

            migrationBuilder.DropForeignKey(
                name: "FK_daily_report_status_history_Users_changed_by_id",
                table: "daily_report_status_history");

            migrationBuilder.DropIndex(
                name: "IX_daily_report_status_history_changed_by_worker_id",
                table: "daily_report_status_history");

            migrationBuilder.DropColumn(
                name: "changed_by_email",
                table: "daily_report_status_history");

            migrationBuilder.DropColumn(
                name: "changed_by_worker_id",
                table: "daily_report_status_history");

            migrationBuilder.AlterColumn<Guid>(
                name: "changed_by_id",
                table: "daily_report_status_history",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_daily_report_status_history_Users_changed_by_id",
                table: "daily_report_status_history",
                column: "changed_by_id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
