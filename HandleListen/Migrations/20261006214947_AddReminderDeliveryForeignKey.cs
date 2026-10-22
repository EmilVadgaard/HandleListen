using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleListen.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderDeliveryForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_ReminderDeliveries_CalendarEvents_CalendarEventId",
                table: "ReminderDeliveries",
                column: "CalendarEventId",
                principalTable: "CalendarEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReminderDeliveries_CalendarEvents_CalendarEventId",
                table: "ReminderDeliveries");
        }
    }
}
