using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandleListen.Migrations
{
    /// <inheritdoc />
    public partial class RecipeBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeGuests");

            migrationBuilder.AddColumn<int>(
                name: "RecipeBookId",
                table: "Recipes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RecipeBookInvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromUserId = table.Column<string>(type: "TEXT", nullable: false),
                    ToUserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeBookInvites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecipeBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeBooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecipeTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeTags_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecipeBookMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipeBookId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeBookMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeBookMembers_RecipeBooks_RecipeBookId",
                        column: x => x.RecipeBookId,
                        principalTable: "RecipeBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeBookMembers_RecipeBookId_UserId",
                table: "RecipeBookMembers",
                columns: new[] { "RecipeBookId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeTags_RecipeId",
                table: "RecipeTags",
                column: "RecipeId");

            // Backfill: give every distinct existing Recipes.UserId its own RecipeBook + membership,
            // and point their recipes at it, before the UserId column is dropped below. Production
            // has no Recipes rows yet, so this only matters for whatever local dev data exists.
            migrationBuilder.Sql(@"
                INSERT INTO RecipeBooks (Id)
                SELECT ROW_NUMBER() OVER (ORDER BY UserId) FROM (SELECT DISTINCT UserId FROM Recipes);
            ");
            migrationBuilder.Sql(@"
                INSERT INTO RecipeBookMembers (RecipeBookId, UserId)
                SELECT ROW_NUMBER() OVER (ORDER BY UserId), UserId FROM (SELECT DISTINCT UserId FROM Recipes);
            ");
            migrationBuilder.Sql(@"
                UPDATE Recipes
                SET RecipeBookId = (SELECT RecipeBookId FROM RecipeBookMembers WHERE RecipeBookMembers.UserId = Recipes.UserId);
            ");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Recipes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeBookInvites");

            migrationBuilder.DropTable(
                name: "RecipeBookMembers");

            migrationBuilder.DropTable(
                name: "RecipeTags");

            migrationBuilder.DropTable(
                name: "RecipeBooks");

            migrationBuilder.DropColumn(
                name: "RecipeBookId",
                table: "Recipes");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Recipes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RecipeGuests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeGuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeGuests_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeGuests_RecipeId_UserId",
                table: "RecipeGuests",
                columns: new[] { "RecipeId", "UserId" },
                unique: true);
        }
    }
}
