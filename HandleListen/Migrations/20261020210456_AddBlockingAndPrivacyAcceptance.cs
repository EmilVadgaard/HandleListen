using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleListen.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockingAndPrivacyAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockedUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BlockerUserId = table.Column<string>(type: "TEXT", nullable: false),
                    BlockedUserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrivacyPolicyAcceptances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacyPolicyAcceptances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockedUsers_BlockerUserId_BlockedUserId",
                table: "BlockedUsers",
                columns: new[] { "BlockerUserId", "BlockedUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedUsers");

            migrationBuilder.DropTable(
                name: "PrivacyPolicyAcceptances");
        }
    }
}
