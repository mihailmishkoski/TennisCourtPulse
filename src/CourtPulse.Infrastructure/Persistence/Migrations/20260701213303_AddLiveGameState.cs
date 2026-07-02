using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourtPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveGameState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentFirstPoints",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentSecondPoints",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Serving",
                table: "Matches",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentFirstPoints",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CurrentSecondPoints",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Serving",
                table: "Matches");
        }
    }
}
