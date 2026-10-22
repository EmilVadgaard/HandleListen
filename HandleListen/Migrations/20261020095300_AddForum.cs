using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleListen.Migrations
{
    /// <inheritdoc />
    public partial class AddForum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForumSearchLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Query = table.Column<string>(type: "TEXT", nullable: false),
                    SearchedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumSearchLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SharedRecipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    SharedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AddedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalRecipeId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedRecipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SharedRecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SharedRecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: true),
                    Unit = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedRecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedRecipeIngredients_SharedRecipes_SharedRecipeId",
                        column: x => x.SharedRecipeId,
                        principalTable: "SharedRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharedRecipeTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SharedRecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedRecipeTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedRecipeTags_SharedRecipes_SharedRecipeId",
                        column: x => x.SharedRecipeId,
                        principalTable: "SharedRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedRecipeIngredients_SharedRecipeId",
                table: "SharedRecipeIngredients",
                column: "SharedRecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedRecipeTags_SharedRecipeId",
                table: "SharedRecipeTags",
                column: "SharedRecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForumSearchLogs");

            migrationBuilder.DropTable(
                name: "SharedRecipeIngredients");

            migrationBuilder.DropTable(
                name: "SharedRecipeTags");

            migrationBuilder.DropTable(
                name: "SharedRecipes");
        }
    }
}
