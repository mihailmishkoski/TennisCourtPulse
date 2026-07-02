using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourtPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalKey = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalKey = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Season = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalEventKey = table.Column<long>(type: "bigint", nullable: false),
                    TournamentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecondPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EventTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Round = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    EventType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsLive = table.Column<bool>(type: "boolean", nullable: false),
                    IsFinished = table.Column<bool>(type: "boolean", nullable: false),
                    FinalResult = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WinnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    MomentumFirstCumulative = table.Column<double>(type: "double precision", nullable: false),
                    MomentumSecondCumulative = table.Column<double>(type: "double precision", nullable: false),
                    MomentumFirstEwma = table.Column<double>(type: "double precision", nullable: false),
                    MomentumSecondEwma = table.Column<double>(type: "double precision", nullable: false),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Players_FirstPlayerId",
                        column: x => x.FirstPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Players_SecondPlayerId",
                        column: x => x.SecondPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Players_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchGames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    GameNumber = table.Column<int>(type: "integer", nullable: false),
                    PlayerServed = table.Column<int>(type: "integer", nullable: false),
                    ServeWinner = table.Column<int>(type: "integer", nullable: true),
                    Score = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MomentumProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchGames_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    ScoreFirst = table.Column<int>(type: "integer", nullable: false),
                    ScoreSecond = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchSets_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MomentumSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    GameNumber = table.Column<int>(type: "integer", nullable: false),
                    PointNumber = table.Column<int>(type: "integer", nullable: false),
                    Beneficiary = table.Column<int>(type: "integer", nullable: false),
                    Delta = table.Column<double>(type: "double precision", nullable: false),
                    Reason = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FirstCumulative = table.Column<double>(type: "double precision", nullable: false),
                    SecondCumulative = table.Column<double>(type: "double precision", nullable: false),
                    FirstEwma = table.Column<double>(type: "double precision", nullable: false),
                    SecondEwma = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MomentumSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MomentumSnapshots_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerMatchStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatPeriod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StatType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    StatName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StatValue = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    StatWon = table.Column<int>(type: "integer", nullable: true),
                    StatTotal = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMatchStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerMatchStatistics_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchGameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PointNumber = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsBreakPoint = table.Column<bool>(type: "boolean", nullable: false),
                    IsSetPoint = table.Column<bool>(type: "boolean", nullable: false),
                    IsMatchPoint = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchPoints_MatchGames_MatchGameId",
                        column: x => x.MatchGameId,
                        principalTable: "MatchGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_EventDate",
                table: "Matches",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ExternalEventKey",
                table: "Matches",
                column: "ExternalEventKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_FirstPlayerId",
                table: "Matches",
                column: "FirstPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_IsLive",
                table: "Matches",
                column: "IsLive");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_SecondPlayerId",
                table: "Matches",
                column: "SecondPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TournamentId",
                table: "Matches",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinnerId",
                table: "Matches",
                column: "WinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchGames_MatchId_SetNumber_GameNumber",
                table: "MatchGames",
                columns: new[] { "MatchId", "SetNumber", "GameNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchPoints_MatchGameId_PointNumber",
                table: "MatchPoints",
                columns: new[] { "MatchGameId", "PointNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchSets_MatchId_SetNumber",
                table: "MatchSets",
                columns: new[] { "MatchId", "SetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MomentumSnapshots_MatchId_SetNumber_GameNumber_PointNumber",
                table: "MomentumSnapshots",
                columns: new[] { "MatchId", "SetNumber", "GameNumber", "PointNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMatchStatistics_MatchId_PlayerId_StatType_StatName",
                table: "PlayerMatchStatistics",
                columns: new[] { "MatchId", "PlayerId", "StatType", "StatName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_ExternalKey",
                table: "Players",
                column: "ExternalKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_ExternalKey",
                table: "Tournaments",
                column: "ExternalKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchPoints");

            migrationBuilder.DropTable(
                name: "MatchSets");

            migrationBuilder.DropTable(
                name: "MomentumSnapshots");

            migrationBuilder.DropTable(
                name: "PlayerMatchStatistics");

            migrationBuilder.DropTable(
                name: "MatchGames");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Tournaments");
        }
    }
}
