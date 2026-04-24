using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityRule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateFieldsAndUserCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new fields to Certificates table
            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Certificates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Thumbprint",
                table: "Certificates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestNumber",
                table: "Certificates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Add CertificateId FK to Users table
            migrationBuilder.AddColumn<int>(
                name: "CertificateId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CertificateId",
                table: "Users",
                column: "CertificateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Certificates_CertificateId",
                table: "Users",
                column: "CertificateId",
                principalTable: "Certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Certificates_CertificateId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CertificateId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CertificateId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "Thumbprint",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "RequestNumber",
                table: "Certificates");
        }
    }
}
