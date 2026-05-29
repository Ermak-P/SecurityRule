using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityRule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartnerNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerNames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePartnerNames",
                columns: table => new
                {
                    PartnersId = table.Column<int>(type: "int", nullable: false),
                    ServicesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePartnerNames", x => new { x.PartnersId, x.ServicesId });
                    table.ForeignKey(
                        name: "FK_ServicePartnerNames_AppServices_ServicesId",
                        column: x => x.ServicesId,
                        principalTable: "AppServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServicePartnerNames_PartnerNames_PartnersId",
                        column: x => x.PartnersId,
                        principalTable: "PartnerNames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerNames_Name",
                table: "PartnerNames",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePartnerNames_ServicesId",
                table: "ServicePartnerNames",
                column: "ServicesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ServicePartnerNames");
            migrationBuilder.DropTable(name: "PartnerNames");
        }
    }
}
