using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorpServe.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class EditRelationRequestProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proposals_RequestId",
                table: "Proposals");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_RequestId",
                table: "Proposals",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proposals_RequestId",
                table: "Proposals");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_RequestId",
                table: "Proposals",
                column: "RequestId",
                unique: true);
        }
    }
}
