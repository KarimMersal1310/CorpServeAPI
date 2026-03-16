using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorpServe.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class RequestModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "RequestAttachmentSequence");

            migrationBuilder.CreateSequence<int>(
                name: "RequestProgressSequence");

            migrationBuilder.CreateSequence<int>(
                name: "RequestSequence");

            migrationBuilder.CreateSequence<int>(
                name: "VendorCertificateSequence");

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

            migrationBuilder.CreateTable(
                name: "Requests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValueSql: "'REQ-' + RIGHT('000' + CAST(NEXT VALUE FOR RequestSequence AS VARCHAR(3)), 3)"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Discription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BudgetMin = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    BudgetMax = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ExpectedDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestStatus = table.Column<int>(type: "int", nullable: false),
                    RequestProgress_ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    RequestProgress_Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestProgress_UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestProgress_Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, defaultValueSql: "'REQP-' + RIGHT('000' + CAST(NEXT VALUE FOR RequestProgressSequence AS VARCHAR(3)), 3)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestAttachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValueSql: "'REQA-' + RIGHT('000' + CAST(NEXT VALUE FOR RequestAttachmentSequence AS VARCHAR(3)), 3)"),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestAttachments_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestAttachments_RequestId",
                table: "RequestAttachments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCertificates_VendorVerifyId",
                table: "VendorCertificates",
                column: "VendorVerifyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestAttachments");

            migrationBuilder.DropTable(
                name: "Requests");

            migrationBuilder.DropTable(
                name: "VendorCertificates");

            migrationBuilder.DropSequence(
                name: "RequestAttachmentSequence");

            migrationBuilder.DropSequence(
                name: "RequestProgressSequence");

            migrationBuilder.DropSequence(
                name: "RequestSequence");

            migrationBuilder.DropSequence(
                name: "VendorCertificateSequence");

        }
    }
}
