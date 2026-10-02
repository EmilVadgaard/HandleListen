using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleListen.Migrations
{
    /// <inheritdoc />
    public partial class AddShoppingListAndListIdToItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ShoppingItems");

            migrationBuilder.AddColumn<int>(
                name: "ShoppingListId",
                table: "ShoppingItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ShoppingLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    GuestIds = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingLists", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingItems_ShoppingListId",
                table: "ShoppingItems",
                column: "ShoppingListId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingItems_ShoppingLists_ShoppingListId",
                table: "ShoppingItems",
                column: "ShoppingListId",
                principalTable: "ShoppingLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingItems_ShoppingLists_ShoppingListId",
                table: "ShoppingItems");

            migrationBuilder.DropTable(
                name: "ShoppingLists");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingItems_ShoppingListId",
                table: "ShoppingItems");

            migrationBuilder.DropColumn(
                name: "ShoppingListId",
                table: "ShoppingItems");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ShoppingItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
