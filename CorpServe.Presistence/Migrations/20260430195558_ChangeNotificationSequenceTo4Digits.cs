using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorpServe.Presistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeNotificationSequenceTo4Digits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "SystemNotifications",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValueSql: "'N-' + RIGHT('0000' + CAST(NEXT VALUE FOR NotificationSequence AS VARCHAR(4)), 4)",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldDefaultValueSql: "'N-' + RIGHT('000' + CAST(NEXT VALUE FOR NotificationSequence AS VARCHAR(3)), 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "SystemNotifications",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValueSql: "'N-' + RIGHT('000' + CAST(NEXT VALUE FOR NotificationSequence AS VARCHAR(3)), 3)",
                oldClrType: typeof(string),
                oldType: "nvarchar(12)",
                oldMaxLength: 12,
                oldDefaultValueSql: "'N-' + RIGHT('0000' + CAST(NEXT VALUE FOR NotificationSequence AS VARCHAR(4)), 4)");
        }
    }
}
