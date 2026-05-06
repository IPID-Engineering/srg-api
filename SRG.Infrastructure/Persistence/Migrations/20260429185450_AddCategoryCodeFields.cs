using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryCodeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FamilyCode",
                table: "Categories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubFamilyCode",
                table: "Categories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_FamilyCode_SubFamilyCode",
                table: "Categories",
                columns: new[] { "FamilyCode", "SubFamilyCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_FamilyCode_SubFamilyCode",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "FamilyCode",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SubFamilyCode",
                table: "Categories");
        }
    }
}
