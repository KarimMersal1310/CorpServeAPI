using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorpServe.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class VendorRequestProgressRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestProgress_VendorId",
                table: "Requests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RequestProgress_VendorId",
                table: "Requests",
                column: "RequestProgress_VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Users_RequestProgress_VendorId",
                table: "Requests",
                column: "RequestProgress_VendorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Users_RequestProgress_VendorId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_RequestProgress_VendorId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RequestProgress_VendorId",
                table: "Requests");
        }
    }
}
