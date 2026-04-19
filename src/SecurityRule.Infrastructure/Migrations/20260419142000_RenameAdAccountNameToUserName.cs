using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityRule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameAdAccountNameToUserName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AdAccountName",
                table: "AppServices",
                newName: "UserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "AppServices",
                newName: "AdAccountName");
        }
    }
}
