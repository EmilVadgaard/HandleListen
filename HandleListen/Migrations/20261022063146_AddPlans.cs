using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleListen.Migrations
{
    /// <inheritdoc />
    public partial class AddPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ActivatedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DayCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Month = table.Column<int>(type: "INTEGER", nullable: true),
                    Year = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanGuests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanGuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanGuests_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanTodos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Done = table.Column<bool>(type: "INTEGER", nullable: false),
                    DayIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    DayOfMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    Time = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanTodos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanTodos_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanGuests_PlanId_UserId",
                table: "PlanGuests",
                columns: new[] { "PlanId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanTodos_PlanId",
                table: "PlanTodos",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanGuests");

            migrationBuilder.DropTable(
                name: "PlanTodos");

            migrationBuilder.DropTable(
                name: "Plans");
        }
    }
}
