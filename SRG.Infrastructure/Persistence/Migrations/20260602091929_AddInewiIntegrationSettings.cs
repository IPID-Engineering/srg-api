using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInewiIntegrationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InewiIntegrationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubcontractorCrewId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfiguredById = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InewiIntegrationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InewiIntegrationSettings_SubcontractorCrews_SubcontractorCr~",
                        column: x => x.SubcontractorCrewId,
                        principalTable: "SubcontractorCrews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InewiIntegrationSettings_Users_ConfiguredById",
                        column: x => x.ConfiguredById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InewiIntegrationSettings_ConfiguredById",
                table: "InewiIntegrationSettings",
                column: "ConfiguredById");

            migrationBuilder.CreateIndex(
                name: "IX_InewiIntegrationSettings_SubcontractorCrewId",
                table: "InewiIntegrationSettings",
                column: "SubcontractorCrewId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InewiIntegrationSettings");
        }
    }
}
