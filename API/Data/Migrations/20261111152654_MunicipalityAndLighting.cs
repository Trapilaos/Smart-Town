using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class MunicipalityAndLighting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "WasteBins",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "WasteBins",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "WasteBins",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "WasteBins",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Municipalities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CenterLatitude = table.Column<double>(type: "REAL", nullable: false),
                    CenterLongitude = table.Column<double>(type: "REAL", nullable: false),
                    DefaultZoom = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    BoundaryGeoJson = table.Column<string>(type: "TEXT", nullable: true),
                    BoundaryIsApproximate = table.Column<bool>(type: "INTEGER", nullable: false),
                    BoundarySource = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BoundaryFetchedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StreetlightCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StreetlightWatts = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipalities", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Municipalities");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "WasteBins");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "WasteBins");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "WasteBins");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "WasteBins");
        }
    }
}
