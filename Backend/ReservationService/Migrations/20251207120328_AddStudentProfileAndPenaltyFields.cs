using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ReservationService.Migrations
{
    public partial class AddStudentProfileAndPenaltyFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PenaltyProcessed",
                table: "Reservations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StudentType",
                table: "Reservations",
                type: "text",
                nullable: false,
                defaultValue: "Lisans");

            migrationBuilder.CreateTable(
                name: "StudentProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentNumber = table.Column<string>(type: "text", nullable: false),
                    StudentType = table.Column<string>(type: "text", nullable: false),
                    PenaltyPoints = table.Column<int>(type: "integer", nullable: false),
                    BanUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    LastNoShowProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_StudentNumber",
                table: "StudentProfiles",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.Sql("UPDATE \"Reservations\" SET \"StudentType\" = 'Lisans' WHERE COALESCE(NULLIF(TRIM(\"StudentType\"), ''), '') = '';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "PenaltyProcessed",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "StudentType",
                table: "Reservations");
        }
    }
}
