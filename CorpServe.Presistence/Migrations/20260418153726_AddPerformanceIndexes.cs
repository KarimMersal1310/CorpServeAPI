using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorpServe.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SystemNotifications_RecipientId",
                table: "SystemNotifications");

            migrationBuilder.DropIndex(
                name: "IX_SLAContracts_VendorId",
                table: "SLAContracts");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ClientId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Proposals_VendorId",
                table: "Proposals");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ChatRoomId",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_VendorVerifications_Status",
                table: "VendorVerifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VendorCategories_VendorId",
                table: "VendorCategories",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_CreatedAt",
                table: "SystemNotifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_RecipientId_IsRead_CreatedAt",
                table: "SystemNotifications",
                columns: new[] { "RecipientId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SLAContracts_VendorId_SLAStatus",
                table: "SLAContracts",
                columns: new[] { "VendorId", "SLAStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ClientId_RequestStatus_CateogryId_CreatedAt",
                table: "Requests",
                columns: new[] { "ClientId", "RequestStatus", "CateogryId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RequestStatus_CateogryId",
                table: "Requests",
                columns: new[] { "RequestStatus", "CateogryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_VendorId_ProposalType_ProposalStatus",
                table: "Proposals",
                columns: new[] { "VendorId", "ProposalType", "ProposalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentStatus_CreatedAt",
                table: "Payments",
                columns: new[] { "PaymentStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatRoomId_SentAt",
                table: "Messages",
                columns: new[] { "ChatRoomId", "SentAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VendorVerifications_Status",
                table: "VendorVerifications");

            migrationBuilder.DropIndex(
                name: "IX_VendorCategories_VendorId",
                table: "VendorCategories");

            migrationBuilder.DropIndex(
                name: "IX_SystemNotifications_CreatedAt",
                table: "SystemNotifications");

            migrationBuilder.DropIndex(
                name: "IX_SystemNotifications_RecipientId_IsRead_CreatedAt",
                table: "SystemNotifications");

            migrationBuilder.DropIndex(
                name: "IX_SLAContracts_VendorId_SLAStatus",
                table: "SLAContracts");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ClientId_RequestStatus_CateogryId_CreatedAt",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_RequestStatus_CateogryId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Proposals_VendorId_ProposalType_ProposalStatus",
                table: "Proposals");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentStatus_CreatedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ChatRoomId_SentAt",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_RecipientId",
                table: "SystemNotifications",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_SLAContracts_VendorId",
                table: "SLAContracts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ClientId",
                table: "Requests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Proposals_VendorId",
                table: "Proposals",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatRoomId",
                table: "Messages",
                column: "ChatRoomId");
        }
    }
}
