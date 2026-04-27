using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityRule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RedesignFirewallRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old FK constraints
            migrationBuilder.DropForeignKey(
                name: "FK_FirewallRules_AppServices_ServiceId",
                table: "FirewallRules");

            migrationBuilder.DropForeignKey(
                name: "FK_FirewallRules_Servers_ServerId",
                table: "FirewallRules");

            // Drop old indexes
            migrationBuilder.DropIndex(
                name: "IX_FirewallRules_ServerId",
                table: "FirewallRules");

            migrationBuilder.DropIndex(
                name: "IX_FirewallRules_ServiceId",
                table: "FirewallRules");

            // Drop old columns
            migrationBuilder.DropColumn(name: "SourceIp",       table: "FirewallRules");
            migrationBuilder.DropColumn(name: "DestinationIp",  table: "FirewallRules");
            migrationBuilder.DropColumn(name: "DestinationPort", table: "FirewallRules");
            migrationBuilder.DropColumn(name: "ServerId",        table: "FirewallRules");
            migrationBuilder.DropColumn(name: "ServiceId",       table: "FirewallRules");

            // Make ExpiresAt nullable
            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "FirewallRules",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            // Add new FK columns
            migrationBuilder.AddColumn<int>(
                name: "SourceServerId",
                table: "FirewallRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SourceServiceId",
                table: "FirewallRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DestinationServerId",
                table: "FirewallRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DestinationServiceId",
                table: "FirewallRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Add new indexes
            migrationBuilder.CreateIndex(
                name: "IX_FirewallRules_SourceServerId",
                table: "FirewallRules",
                column: "SourceServerId");

            migrationBuilder.CreateIndex(
                name: "IX_FirewallRules_SourceServiceId",
                table: "FirewallRules",
                column: "SourceServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_FirewallRules_DestinationServerId",
                table: "FirewallRules",
                column: "DestinationServerId");

            migrationBuilder.CreateIndex(
                name: "IX_FirewallRules_DestinationServiceId",
                table: "FirewallRules",
                column: "DestinationServiceId");

            // Add new FK constraints
            migrationBuilder.AddForeignKey(
                name: "FK_FirewallRules_Servers_SourceServerId",
                table: "FirewallRules",
                column: "SourceServerId",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FirewallRules_AppServices_SourceServiceId",
                table: "FirewallRules",
                column: "SourceServiceId",
                principalTable: "AppServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FirewallRules_Servers_DestinationServerId",
                table: "FirewallRules",
                column: "DestinationServerId",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FirewallRules_AppServices_DestinationServiceId",
                table: "FirewallRules",
                column: "DestinationServiceId",
                principalTable: "AppServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new FK constraints
            migrationBuilder.DropForeignKey(
                name: "FK_FirewallRules_Servers_SourceServerId",
                table: "FirewallRules");

            migrationBuilder.DropForeignKey(
                name: "FK_FirewallRules_AppServices_SourceServiceId",
                table: "FirewallRules");

            migrationBuilder.DropForeignKey(
                name: "FK_FirewallRules_Servers_DestinationServerId",
                table: "FirewallRules");

            migrationBuilder.DropForeignKey(
                name: "FK_FirewallRules_AppServices_DestinationServiceId",
                table: "FirewallRules");

            // Drop new indexes
            migrationBuilder.DropIndex(name: "IX_FirewallRules_SourceServerId",       table: "FirewallRules");
            migrationBuilder.DropIndex(name: "IX_FirewallRules_SourceServiceId",      table: "FirewallRules");
            migrationBuilder.DropIndex(name: "IX_FirewallRules_DestinationServerId",  table: "FirewallRules");
            migrationBuilder.DropIndex(name: "IX_FirewallRules_DestinationServiceId", table: "FirewallRules");

            // Drop new columns
            migrationBuilder.DropColumn(name: "SourceServerId",       table: "FirewallRules");
            migrationBuilder.DropColumn(name: "SourceServiceId",      table: "FirewallRules");
            migrationBuilder.DropColumn(name: "DestinationServerId",  table: "FirewallRules");
            migrationBuilder.DropColumn(name: "DestinationServiceId", table: "FirewallRules");

            // Restore ExpiresAt non-nullable
            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "FirewallRules",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            // Restore old columns
            migrationBuilder.AddColumn<string>(
                name: "SourceIp",
                table: "FirewallRules",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationIp",
                table: "FirewallRules",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DestinationPort",
                table: "FirewallRules",
                type: "int",
                nullable: true);

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

            // Restore old indexes
            migrationBuilder.CreateIndex(name: "IX_FirewallRules_ServerId",  table: "FirewallRules", column: "ServerId");
            migrationBuilder.CreateIndex(name: "IX_FirewallRules_ServiceId", table: "FirewallRules", column: "ServiceId");

            // Restore old FK constraints
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
    }
}
