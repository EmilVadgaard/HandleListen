using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleListen.Migrations
{
    /// <inheritdoc />
    public partial class AddKnownItemRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnownItemRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SourceDescription = table.Column<string>(type: "TEXT", nullable: true),
                    RequestedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SeenCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Resolved = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownItemRequests", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnownItemRequests");
        }
    }
}
