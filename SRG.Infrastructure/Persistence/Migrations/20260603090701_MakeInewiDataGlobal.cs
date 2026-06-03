using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeInewiDataGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InewiIntegrationSettings_SubcontractorCrews_SubcontractorCr~",
                table: "InewiIntegrationSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_InewiRecords_SubcontractorCrews_SubcontractorCrewId",
                table: "InewiRecords");

            migrationBuilder.RenameColumn(
                name: "SubcontractorCrewId",
                table: "InewiRecords",
                newName: "SubcontractorId");

            migrationBuilder.RenameIndex(
                name: "IX_InewiRecords_SubcontractorCrewId_Date_WorkerName",
                table: "InewiRecords",
                newName: "IX_InewiRecords_SubcontractorId_Date_WorkerName");

            migrationBuilder.RenameColumn(
                name: "SubcontractorCrewId",
                table: "InewiIntegrationSettings",
                newName: "SubcontractorId");

            migrationBuilder.RenameIndex(
                name: "IX_InewiIntegrationSettings_SubcontractorCrewId",
                table: "InewiIntegrationSettings",
                newName: "IX_InewiIntegrationSettings_SubcontractorId");

            migrationBuilder.AddForeignKey(
                name: "FK_InewiIntegrationSettings_Users_SubcontractorId",
                table: "InewiIntegrationSettings",
                column: "SubcontractorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InewiRecords_Users_SubcontractorId",
                table: "InewiRecords",
                column: "SubcontractorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InewiIntegrationSettings_Users_SubcontractorId",
                table: "InewiIntegrationSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_InewiRecords_Users_SubcontractorId",
                table: "InewiRecords");

            migrationBuilder.RenameColumn(
                name: "SubcontractorId",
                table: "InewiRecords",
                newName: "SubcontractorCrewId");

            migrationBuilder.RenameIndex(
                name: "IX_InewiRecords_SubcontractorId_Date_WorkerName",
                table: "InewiRecords",
                newName: "IX_InewiRecords_SubcontractorCrewId_Date_WorkerName");

            migrationBuilder.RenameColumn(
                name: "SubcontractorId",
                table: "InewiIntegrationSettings",
                newName: "SubcontractorCrewId");

            migrationBuilder.RenameIndex(
                name: "IX_InewiIntegrationSettings_SubcontractorId",
                table: "InewiIntegrationSettings",
                newName: "IX_InewiIntegrationSettings_SubcontractorCrewId");

            migrationBuilder.AddForeignKey(
                name: "FK_InewiIntegrationSettings_SubcontractorCrews_SubcontractorCr~",
                table: "InewiIntegrationSettings",
                column: "SubcontractorCrewId",
                principalTable: "SubcontractorCrews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InewiRecords_SubcontractorCrews_SubcontractorCrewId",
                table: "InewiRecords",
                column: "SubcontractorCrewId",
                principalTable: "SubcontractorCrews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
