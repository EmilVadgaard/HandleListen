using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleListen.Migrations
{
    /// <inheritdoc />
    public partial class AddKnownItemsAndCreatedByUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "ShoppingItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KnownItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CanonicalName = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnownItemAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KnownItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownItemAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnownItemAliases_KnownItems_KnownItemId",
                        column: x => x.KnownItemId,
                        principalTable: "KnownItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnownItemAliases_Alias",
                table: "KnownItemAliases",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnownItemAliases_KnownItemId",
                table: "KnownItemAliases",
                column: "KnownItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnownItemAliases");

            migrationBuilder.DropTable(
                name: "KnownItems");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ShoppingItems");
        }
    }
}
