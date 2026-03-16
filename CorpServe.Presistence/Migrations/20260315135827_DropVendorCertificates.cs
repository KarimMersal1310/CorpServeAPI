using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorpServe.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class DropVendorCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorCertificates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VendorCertificates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValueSql: "'VC-' + RIGHT('000' + CAST(NEXT VALUE FOR VendorCertificateSequence AS VARCHAR(3)), 3)"),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertificateType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VendorVerifyId = table.Column<string>(type: "nvarchar(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorCertificates_VendorVerifications_VendorVerifyId",
                        column: x => x.VendorVerifyId,
                        principalTable: "VendorVerifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorCertificates_VendorVerifyId",
                table: "VendorCertificates",
                column: "VendorVerifyId");
        }
    }
}
