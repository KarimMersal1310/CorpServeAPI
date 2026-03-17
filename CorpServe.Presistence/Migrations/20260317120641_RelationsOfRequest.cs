using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorpServe.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class RelationsOfRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "AIEstimationSequence");

            migrationBuilder.AddColumn<string>(
                name: "CateogryId",
                table: "Requests",
                type: "nvarchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Requests",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AIEstimation",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValueSql: "'AIE-' + RIGHT('000' + CAST(NEXT VALUE FOR AIEstimationSequence AS VARCHAR(3)), 3)"),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Confidence = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIEstimation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIEstimation_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CateogryId",
                table: "Requests",
                column: "CateogryId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ClientId",
                table: "Requests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AIEstimation_RequestId",
                table: "AIEstimation",
                column: "RequestId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Categories_CateogryId",
                table: "Requests",
                column: "CateogryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_ClientId",
                table: "Requests",
                column: "ClientId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Categories_CateogryId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_ClientId",
                table: "Requests");

            migrationBuilder.DropTable(
                name: "AIEstimation");

            migrationBuilder.DropIndex(
                name: "IX_Requests_CateogryId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ClientId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "CateogryId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Requests");

            migrationBuilder.DropSequence(
                name: "AIEstimationSequence");
        }
    }
}
