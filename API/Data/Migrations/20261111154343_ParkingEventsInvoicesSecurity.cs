using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class ParkingEventsInvoicesSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    AtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventInterests",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventInterests", x => new { x.EventId, x.UserId });
                    table.ForeignKey(
                        name: "FK_EventInterests_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "ParkingSpaces",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "ParkingSpaces",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            // Existing spaces must stay reservable, so they default to active.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ParkingSpaces",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            // Existing rows sit at (0, 0) until the seed backfill copies their coordinates.
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "ParkingSpaces",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "ParkingSpaces",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ParkingSpaces",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Street");

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Invoices",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayerName",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentCode",
                table: "Invoices",
                type: "TEXT",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptReference",
                table: "Invoices",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Events",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Events",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Events",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Events that already exist were public, so they stay published.
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Events",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Events",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Events",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Events",
                type: "REAL",
                nullable: true);

            // Keep the existing "interested" marks: the old Events.InterestedUsers JSON array becomes one
            // EventInterests row per user. json_valid guards against rows that were never written as JSON.
            migrationBuilder.Sql(@"
INSERT OR IGNORE INTO EventInterests (EventId, UserId, CreatedAt)
SELECT e.Id, j.value, CURRENT_TIMESTAMP
FROM Events AS e,
     json_each(CASE WHEN json_valid(e.InterestedUsers) THEN e.InterestedUsers ELSE '[]' END) AS j
WHERE j.type = 'text';");

            migrationBuilder.DropColumn(
                name: "InterestedUsers",
                table: "Events");

            // The real creation time of existing events is unknown, so they record the time of this migration.
            migrationBuilder.Sql(@"UPDATE Events SET CreatedAt = CURRENT_TIMESTAMP;");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_AtUtc",
                table: "AuditEntries",
                column: "AtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "EventInterests");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ParkingSpaces");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PayerName",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReceiptReference",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "InterestedUsers",
                table: "Events",
                type: "TEXT",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNumber",
                table: "Invoices",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 32);
        }
    }
}
