using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityRule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFirewallRuleServerServiceLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SourceIp",
                table: "FirewallRules",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(45)",
                oldMaxLength: 45);

            migrationBuilder.AlterColumn<string>(
                name: "DestinationIp",
                table: "FirewallRules",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(45)",
                oldMaxLength: 45);

            migrationBuilder.AddColumn<int>(
                name: "ServerId",
                table: "FirewallRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "FirewallRules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FirewallRules_ServerId",
                table: "FirewallRules",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_FirewallRules_ServiceId",
                table: "FirewallRules",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_FirewallRules_AppServices_ServiceId",
                table: "FirewallRules",
                column: "ServiceId",
                principalTable: "AppServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FirewallRules_Servers_ServerId",
                table: "FirewallRules",
                column: "ServerId",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FirewallRules_AppServices_ServiceId",
                table: "FirewallRules");

            migrationBuilder.DropForeignKey(
                name: "FK_FirewallRules_Servers_ServerId",
                table: "FirewallRules");

            migrationBuilder.DropIndex(
                name: "IX_FirewallRules_ServerId",
                table: "FirewallRules");

            migrationBuilder.DropIndex(
                name: "IX_FirewallRules_ServiceId",
                table: "FirewallRules");

            migrationBuilder.DropColumn(
                name: "ServerId",
                table: "FirewallRules");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "FirewallRules");

            migrationBuilder.AlterColumn<string>(
                name: "SourceIp",
                table: "FirewallRules",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DestinationIp",
                table: "FirewallRules",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(45)",
                oldMaxLength: 45,
                oldNullable: true);
        }
    }
}
