using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityRule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortAndFirewallFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "AppServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DestinationPort",
                table: "FirewallRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Protocol",
                table: "FirewallRules",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "FirewallRules",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "FirewallRules",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Port",
                table: "AppServices");

            migrationBuilder.DropColumn(
                name: "DestinationPort",
                table: "FirewallRules");

            migrationBuilder.DropColumn(
                name: "Protocol",
                table: "FirewallRules");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "FirewallRules");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "FirewallRules");
        }
    }
}
