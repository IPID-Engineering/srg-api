using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DailyReportComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_report_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    daily_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section = table.Column<int>(type: "integer", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    parent_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_report_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_daily_report_comments_DailyReports_daily_report_id",
                        column: x => x.daily_report_id,
                        principalTable: "DailyReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_report_comments_Users_author_id",
                        column: x => x.author_id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_daily_report_comments_daily_report_comments_parent_comment_~",
                        column: x => x.parent_comment_id,
                        principalTable: "daily_report_comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_report_comments_author_id",
                table: "daily_report_comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_daily_report_comments_daily_report_id",
                table: "daily_report_comments",
                column: "daily_report_id");

            migrationBuilder.CreateIndex(
                name: "IX_daily_report_comments_parent_comment_id",
                table: "daily_report_comments",
                column: "parent_comment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_report_comments");
        }
    }
}
