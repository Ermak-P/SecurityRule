using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityRule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdAccountGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AdAccountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdAccountGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdAccountGroups_AdAccounts_AdAccountId",
                        column: x => x.AdAccountId,
                        principalTable: "AdAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "AdAccountId",
                table: "AppServices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppServices_AdAccountId",
                table: "AppServices",
                column: "AdAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AdAccountGroups_AdAccountId",
                table: "AdAccountGroups",
                column: "AdAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppServices_AdAccounts_AdAccountId",
                table: "AppServices",
                column: "AdAccountId",
                principalTable: "AdAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppServices_AdAccounts_AdAccountId",
                table: "AppServices");

            migrationBuilder.DropIndex(
                name: "IX_AppServices_AdAccountId",
                table: "AppServices");

            migrationBuilder.DropColumn(
                name: "AdAccountId",
                table: "AppServices");

            migrationBuilder.DropTable(
                name: "AdAccountGroups");

            migrationBuilder.DropTable(
                name: "AdAccounts");
        }
    }
}
