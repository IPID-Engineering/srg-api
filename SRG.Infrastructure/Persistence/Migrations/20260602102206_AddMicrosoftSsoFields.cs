using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMicrosoftSsoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActivationToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivationTokenExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMicrosoftLinked",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MicrosoftSubjectId",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActivationTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsMicrosoftLinked",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MicrosoftSubjectId",
                table: "Users");
        }
    }
}
