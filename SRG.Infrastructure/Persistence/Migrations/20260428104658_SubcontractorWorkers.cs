using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SubcontractorWorkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Workers");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkerId",
                table: "WorkHours",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "SubcontractorWorkerId",
                table: "WorkHours",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Workers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Workers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ProjectSubcontractors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubcontractorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSubcontractors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectSubcontractors_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectSubcontractors_Users_SubcontractorId",
                        column: x => x.SubcontractorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractorWorkers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LastName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SubcontractorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorWorkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubcontractorWorkers_Users_SubcontractorId",
                        column: x => x.SubcontractorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkHours_SubcontractorWorkerId",
                table: "WorkHours",
                column: "SubcontractorWorkerId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkHours_ExactlyOneWorker",
                table: "WorkHours",
                sql: "(\"WorkerId\" IS NOT NULL AND \"SubcontractorWorkerId\" IS NULL) OR (\"WorkerId\" IS NULL AND \"SubcontractorWorkerId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_CreatedById",
                table: "Workers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSubcontractors_ProjectId_SubcontractorId",
                table: "ProjectSubcontractors",
                columns: new[] { "ProjectId", "SubcontractorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSubcontractors_SubcontractorId",
                table: "ProjectSubcontractors",
                column: "SubcontractorId");

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorWorkers_SubcontractorId",
                table: "SubcontractorWorkers",
                column: "SubcontractorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workers_Users_CreatedById",
                table: "Workers",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkHours_SubcontractorWorkers_SubcontractorWorkerId",
                table: "WorkHours",
                column: "SubcontractorWorkerId",
                principalTable: "SubcontractorWorkers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workers_Users_CreatedById",
                table: "Workers");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkHours_SubcontractorWorkers_SubcontractorWorkerId",
                table: "WorkHours");

            migrationBuilder.DropTable(
                name: "ProjectSubcontractors");

            migrationBuilder.DropTable(
                name: "SubcontractorWorkers");

            migrationBuilder.DropIndex(
                name: "IX_WorkHours_SubcontractorWorkerId",
                table: "WorkHours");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkHours_ExactlyOneWorker",
                table: "WorkHours");

            migrationBuilder.DropIndex(
                name: "IX_Workers_CreatedById",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "SubcontractorWorkerId",
                table: "WorkHours");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Workers");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkerId",
                table: "WorkHours",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Workers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");
        }
    }
}
