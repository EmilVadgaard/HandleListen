using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleListen.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurrenceAndEmailReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReminderDeliveries_CalendarEventId_UserId",
                table: "ReminderDeliveries");

            migrationBuilder.AddColumn<DateOnly>(
                name: "OccurrenceDate",
                table: "ReminderDeliveries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // Backfill: every existing delivery row was recorded before recurrence existed, so
            // its event only ever had one occurrence - the event's own Date.
            migrationBuilder.Sql(
                """
                UPDATE "ReminderDeliveries"
                SET "OccurrenceDate" = (
                    SELECT "Date" FROM "CalendarEvents" WHERE "CalendarEvents"."Id" = "ReminderDeliveries"."CalendarEventId"
                );
                """);

            migrationBuilder.AddColumn<bool>(
                name: "EmailReminderEnabled",
                table: "CalendarEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Existing events relied on push being implicitly "on" whenever a reminder was set -
            // preserve that behavior instead of silently going dark for everyone on deploy.
            migrationBuilder.AddColumn<bool>(
                name: "PushReminderEnabled",
                table: "CalendarEvents",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "Recurrence",
                table: "CalendarEvents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CalendarEventExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CalendarEventId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExceptionDate = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEventExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarEventExceptions_CalendarEvents_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "CalendarEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DigestDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    SentDate = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigestDeliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DigestPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigestPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailReminderDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CalendarEventId = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurrenceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailReminderDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailReminderDeliveries_CalendarEvents_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "CalendarEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderDeliveries_CalendarEventId_OccurrenceDate_UserId",
                table: "ReminderDeliveries",
                columns: new[] { "CalendarEventId", "OccurrenceDate", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEventExceptions_CalendarEventId_ExceptionDate",
                table: "CalendarEventExceptions",
                columns: new[] { "CalendarEventId", "ExceptionDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigestDeliveries_UserId_Frequency_SentDate",
                table: "DigestDeliveries",
                columns: new[] { "UserId", "Frequency", "SentDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigestPreferences_UserId",
                table: "DigestPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailReminderDeliveries_CalendarEventId_OccurrenceDate_UserId",
                table: "EmailReminderDeliveries",
                columns: new[] { "CalendarEventId", "OccurrenceDate", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarEventExceptions");

            migrationBuilder.DropTable(
                name: "DigestDeliveries");

            migrationBuilder.DropTable(
                name: "DigestPreferences");

            migrationBuilder.DropTable(
                name: "EmailReminderDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_ReminderDeliveries_CalendarEventId_OccurrenceDate_UserId",
                table: "ReminderDeliveries");

            migrationBuilder.DropColumn(
                name: "OccurrenceDate",
                table: "ReminderDeliveries");

            migrationBuilder.DropColumn(
                name: "EmailReminderEnabled",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "PushReminderEnabled",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "Recurrence",
                table: "CalendarEvents");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderDeliveries_CalendarEventId_UserId",
                table: "ReminderDeliveries",
                columns: new[] { "CalendarEventId", "UserId" },
                unique: true);
        }
    }
}
