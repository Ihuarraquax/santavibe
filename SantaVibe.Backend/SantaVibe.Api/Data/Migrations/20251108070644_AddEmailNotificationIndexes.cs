using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SantaVibe.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotificationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index for efficiently querying pending notifications
            migrationBuilder.CreateIndex(
                name: "IX_EmailNotifications_Processing",
                table: "EmailNotifications",
                columns: new[] { "SentAt", "ScheduledAt", "AttemptCount" },
                filter: "\"SentAt\" IS NULL");

            // Index for looking up notifications by recipient and group
            migrationBuilder.CreateIndex(
                name: "IX_EmailNotifications_Recipient",
                table: "EmailNotifications",
                columns: new[] { "RecipientUserId", "GroupId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailNotifications_Processing",
                table: "EmailNotifications");

            migrationBuilder.DropIndex(
                name: "IX_EmailNotifications_Recipient",
                table: "EmailNotifications");
        }
    }
}
