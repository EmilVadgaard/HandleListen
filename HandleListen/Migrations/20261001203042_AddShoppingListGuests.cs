using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleListen.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingListGuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestIds",
                table: "ShoppingLists");

            migrationBuilder.CreateTable(
                name: "ShoppingListGuests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShoppingListId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingListGuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingListGuests_ShoppingLists_ShoppingListId",
                        column: x => x.ShoppingListId,
                        principalTable: "ShoppingLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListGuests_ShoppingListId_UserId",
                table: "ShoppingListGuests",
                columns: new[] { "ShoppingListId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingListGuests");

            migrationBuilder.AddColumn<string>(
                name: "GuestIds",
                table: "ShoppingLists",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
