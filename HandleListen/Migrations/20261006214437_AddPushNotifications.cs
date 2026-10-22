using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleListen.Migrations
{
    /// <inheritdoc />
    public partial class AddPushNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReminderDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CalendarEventId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderDeliveries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_Token",
                table: "PushSubscriptions",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReminderDeliveries_CalendarEventId_UserId",
                table: "ReminderDeliveries",
                columns: new[] { "CalendarEventId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PushSubscriptions");

            migrationBuilder.DropTable(
                name: "ReminderDeliveries");
        }
    }
}
