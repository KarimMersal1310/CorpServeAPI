using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorpServe.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class ProposalModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "ProposalSequence");

            migrationBuilder.CreateSequence<int>(
                name: "SLAContractSequence");

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValueSql: "'P-' + RIGHT('000' + CAST(NEXT VALUE FOR ProposalSequence AS VARCHAR(3)), 3)"),
                    ProposalStatus = table.Column<int>(type: "int", nullable: false),
                    ProposalType = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProposedPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProposedDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClientResponseAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VendorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proposals_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Proposals_Users_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SLAContracts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValueSql: "'SLA-' + RIGHT('000' + CAST(NEXT VALUE FOR SLAContractSequence AS VARCHAR(3)), 3)"),
                    ContractPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SLAStatus = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ProposalId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SLAContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SLAContracts_Proposals_ProposalId",
                        column: x => x.ProposalId,
                        principalTable: "Proposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SLAContracts_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SLAContracts_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SLAContracts_Users_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_RequestId",
                table: "Proposals",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_VendorId",
                table: "Proposals",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_SLAContracts_ClientId",
                table: "SLAContracts",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_SLAContracts_ProposalId",
                table: "SLAContracts",
                column: "ProposalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SLAContracts_RequestId",
                table: "SLAContracts",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SLAContracts_VendorId",
                table: "SLAContracts",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SLAContracts");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropSequence(
                name: "ProposalSequence");

            migrationBuilder.DropSequence(
                name: "SLAContractSequence");
        }
    }
}
