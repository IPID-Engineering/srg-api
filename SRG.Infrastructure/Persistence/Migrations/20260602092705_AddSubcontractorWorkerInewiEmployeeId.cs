using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontractorWorkerInewiEmployeeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InewiEmployeeId",
                table: "SubcontractorWorkers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InewiEmployeeId",
                table: "SubcontractorWorkers");
        }
    }
}
