using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityRule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceFirewallRulesWithServiceConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FirewallRules");

            migrationBuilder.CreateTable(
                name: "ServiceConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceServerId = table.Column<int>(type: "int", nullable: true),
                    SourceServiceId = table.Column<int>(type: "int", nullable: true),
                    DestinationServerId = table.Column<int>(type: "int", nullable: true),
                    DestinationServiceId = table.Column<int>(type: "int", nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceConnections_AppServices_DestinationServiceId",
                        column: x => x.DestinationServiceId,
                        principalTable: "AppServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceConnections_AppServices_SourceServiceId",
                        column: x => x.SourceServiceId,
                        principalTable: "AppServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceConnections_Servers_DestinationServerId",
                        column: x => x.DestinationServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceConnections_Servers_SourceServerId",
                        column: x => x.SourceServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceConnections_DestinationServiceId",
                table: "ServiceConnections",
                column: "DestinationServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceConnections_DestinationServerId",
                table: "ServiceConnections",
                column: "DestinationServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceConnections_SourceServiceId",
                table: "ServiceConnections",
                column: "SourceServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceConnections_SourceServerId",
                table: "ServiceConnections",
                column: "SourceServerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceConnections");

            migrationBuilder.CreateTable(
                name: "FirewallRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestinationServiceId = table.Column<int>(type: "int", nullable: false),
                    DestinationServerId = table.Column<int>(type: "int", nullable: false),
                    SourceServiceId = table.Column<int>(type: "int", nullable: false),
                    SourceServerId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Protocol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirewallRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirewallRules_AppServices_DestinationServiceId",
                        column: x => x.DestinationServiceId,
                        principalTable: "AppServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FirewallRules_AppServices_SourceServiceId",
                        column: x => x.SourceServiceId,
                        principalTable: "AppServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FirewallRules_Servers_DestinationServerId",
                        column: x => x.DestinationServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FirewallRules_Servers_SourceServerId",
                        column: x => x.SourceServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FirewallRules_DestinationServiceId",
                table: "FirewallRules",
                column: "DestinationServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_FirewallRules_DestinationServerId",
                table: "FirewallRules",
                column: "DestinationServerId");

            migrationBuilder.CreateIndex(
                name: "IX_FirewallRules_SourceServiceId",
                table: "FirewallRules",
                column: "SourceServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_FirewallRules_SourceServerId",
                table: "FirewallRules",
                column: "SourceServerId");
        }
    }
}
