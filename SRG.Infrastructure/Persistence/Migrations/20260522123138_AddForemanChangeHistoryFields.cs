using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddForemanChangeHistoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ChangedById",
                table: "DailyReportChangeHistory",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "ChangedByEmail",
                table: "DailyReportChangeHistory",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ChangedByWorkerId",
                table: "DailyReportChangeHistory",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangedByEmail",
                table: "DailyReportChangeHistory");

            migrationBuilder.DropColumn(
                name: "ChangedByWorkerId",
                table: "DailyReportChangeHistory");

            migrationBuilder.AlterColumn<Guid>(
                name: "ChangedById",
                table: "DailyReportChangeHistory",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
