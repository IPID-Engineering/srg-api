using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddForemanLoginFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultPassword",
                table: "SubcontractorWorkers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "SubcontractorWorkers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "SubcontractorWorkers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "SubcontractorWorkers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultPassword",
                table: "SubcontractorWorkers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "SubcontractorWorkers");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "SubcontractorWorkers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "SubcontractorWorkers");
        }
    }
}
