using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueVerificationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationCode",
                table: "Issues",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerificationCode",
                table: "Issues");
        }
    }
}
