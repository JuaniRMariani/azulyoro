using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Azulyoro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.CreateTable(
                name: "competitions",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ext_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    country = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_competitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_states",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_ok_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ext_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    short_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    logo_url = table.Column<string>(type: "text", nullable: true),
                    founded = table.Column<int>(type: "integer", nullable: true),
                    venue_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    venue_city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    is_tracked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_teams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fixtures",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ext_id = table.Column<int>(type: "integer", nullable: false),
                    competition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    round = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    date_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    elapsed = table.Column<int>(type: "integer", nullable: true),
                    venue_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    home_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    away_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    home_goals = table.Column<int>(type: "integer", nullable: true),
                    away_goals = table.Column<int>(type: "integer", nullable: true),
                    ht_home = table.Column<int>(type: "integer", nullable: true),
                    ht_away = table.Column<int>(type: "integer", nullable: true),
                    ft_home = table.Column<int>(type: "integer", nullable: true),
                    ft_away = table.Column<int>(type: "integer", nullable: true),
                    is_boca = table.Column<bool>(type: "boolean", nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fixtures", x => x.id);
                    table.ForeignKey(
                        name: "fk_fixtures_competitions_competition_id",
                        column: x => x.competition_id,
                        principalSchema: "app",
                        principalTable: "competitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fixtures_seasons_season_id",
                        column: x => x.season_id,
                        principalSchema: "app",
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fixtures_teams_away_team_id",
                        column: x => x.away_team_id,
                        principalSchema: "app",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fixtures_teams_home_team_id",
                        column: x => x.home_team_id,
                        principalSchema: "app",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "players",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ext_id = table.Column<int>(type: "integer", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    firstname = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    lastname = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    nationality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    weight = table.Column<int>(type: "integer", nullable: true),
                    position = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    number = table.Column<int>(type: "integer", nullable: true),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_players", x => x.id);
                    table.ForeignKey(
                        name: "fk_players_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "app",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "standings",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    goals_diff = table.Column<int>(type: "integer", nullable: false),
                    played = table.Column<int>(type: "integer", nullable: false),
                    win = table.Column<int>(type: "integer", nullable: false),
                    draw = table.Column<int>(type: "integer", nullable: false),
                    lose = table.Column<int>(type: "integer", nullable: false),
                    goals_for = table.Column<int>(type: "integer", nullable: false),
                    goals_against = table.Column<int>(type: "integer", nullable: false),
                    form = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    group_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false, defaultValue: ""),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_standings", x => x.id);
                    table.ForeignKey(
                        name: "fk_standings_competitions_competition_id",
                        column: x => x.competition_id,
                        principalSchema: "app",
                        principalTable: "competitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_standings_seasons_season_id",
                        column: x => x.season_id,
                        principalSchema: "app",
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_standings_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "app",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fixture_events",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fixture_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ext_seq = table.Column<int>(type: "integer", nullable: false),
                    minute = table.Column<int>(type: "integer", nullable: false),
                    extra_minute = table.Column<int>(type: "integer", nullable: true),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assist_player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    detail = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    comments = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fixture_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_fixture_events_fixtures_fixture_id",
                        column: x => x.fixture_id,
                        principalSchema: "app",
                        principalTable: "fixtures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fixture_lineups",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fixture_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    formation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    coach_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fixture_lineups", x => x.id);
                    table.ForeignKey(
                        name: "fk_fixture_lineups_fixtures_fixture_id",
                        column: x => x.fixture_id,
                        principalSchema: "app",
                        principalTable: "fixtures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fixture_lineups_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "app",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fixture_player_stats",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fixture_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    minutes = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    goals = table.Column<int>(type: "integer", nullable: false),
                    assists = table.Column<int>(type: "integer", nullable: false),
                    shots_total = table.Column<int>(type: "integer", nullable: false),
                    shots_on = table.Column<int>(type: "integer", nullable: false),
                    passes = table.Column<int>(type: "integer", nullable: false),
                    passes_accuracy = table.Column<int>(type: "integer", nullable: true),
                    tackles = table.Column<int>(type: "integer", nullable: false),
                    yellow = table.Column<int>(type: "integer", nullable: false),
                    red = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fixture_player_stats", x => x.id);
                    table.ForeignKey(
                        name: "fk_fixture_player_stats_fixtures_fixture_id",
                        column: x => x.fixture_id,
                        principalSchema: "app",
                        principalTable: "fixtures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_season_stats",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    competition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    appearances = table.Column<int>(type: "integer", nullable: false),
                    minutes = table.Column<int>(type: "integer", nullable: false),
                    goals = table.Column<int>(type: "integer", nullable: false),
                    assists = table.Column<int>(type: "integer", nullable: false),
                    yellow = table.Column<int>(type: "integer", nullable: false),
                    red = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_season_stats", x => x.id);
                    table.ForeignKey(
                        name: "fk_player_season_stats_competitions_competition_id",
                        column: x => x.competition_id,
                        principalSchema: "app",
                        principalTable: "competitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_player_season_stats_players_player_id",
                        column: x => x.player_id,
                        principalSchema: "app",
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_player_season_stats_seasons_season_id",
                        column: x => x.season_id,
                        principalSchema: "app",
                        principalTable: "seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fixture_lineup_players",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lineup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_starter = table.Column<bool>(type: "boolean", nullable: false),
                    grid = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    number = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fixture_lineup_players", x => x.id);
                    table.ForeignKey(
                        name: "fk_fixture_lineup_players_fixture_lineups_lineup_id",
                        column: x => x.lineup_id,
                        principalSchema: "app",
                        principalTable: "fixture_lineups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_competitions_ext_id",
                schema: "app",
                table: "competitions",
                column: "ext_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fixture_events_fixture_id_ext_seq",
                schema: "app",
                table: "fixture_events",
                columns: new[] { "fixture_id", "ext_seq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fixture_lineup_players_lineup_id",
                schema: "app",
                table: "fixture_lineup_players",
                column: "lineup_id");

            migrationBuilder.CreateIndex(
                name: "ix_fixture_lineups_fixture_id",
                schema: "app",
                table: "fixture_lineups",
                column: "fixture_id");

            migrationBuilder.CreateIndex(
                name: "ix_fixture_lineups_team_id",
                schema: "app",
                table: "fixture_lineups",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_fixture_player_stats_fixture_id_player_id",
                schema: "app",
                table: "fixture_player_stats",
                columns: new[] { "fixture_id", "player_id" });

            migrationBuilder.CreateIndex(
                name: "ix_fixtures_away_team_id",
                schema: "app",
                table: "fixtures",
                column: "away_team_id");

            migrationBuilder.CreateIndex(
                name: "ix_fixtures_competition_id",
                schema: "app",
                table: "fixtures",
                column: "competition_id");

            migrationBuilder.CreateIndex(
                name: "ix_fixtures_date_utc",
                schema: "app",
                table: "fixtures",
                column: "date_utc");

            migrationBuilder.CreateIndex(
                name: "ix_fixtures_ext_id",
                schema: "app",
                table: "fixtures",
                column: "ext_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fixtures_home_team_id",
                schema: "app",
                table: "fixtures",
                column: "home_team_id");

            migrationBuilder.CreateIndex(
                name: "ix_fixtures_is_boca_date_utc",
                schema: "app",
                table: "fixtures",
                columns: new[] { "is_boca", "date_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_fixtures_season_id",
                schema: "app",
                table: "fixtures",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_fixtures_status",
                schema: "app",
                table: "fixtures",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_player_season_stats_competition_id",
                schema: "app",
                table: "player_season_stats",
                column: "competition_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_season_stats_player_id_competition_id_season_id",
                schema: "app",
                table: "player_season_stats",
                columns: new[] { "player_id", "competition_id", "season_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_season_stats_season_id",
                schema: "app",
                table: "player_season_stats",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_players_ext_id",
                schema: "app",
                table: "players",
                column: "ext_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_players_team_id",
                schema: "app",
                table: "players",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_year",
                schema: "app",
                table: "seasons",
                column: "year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_standings_competition_id_season_id_team_id_group_name",
                schema: "app",
                table: "standings",
                columns: new[] { "competition_id", "season_id", "team_id", "group_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_standings_season_id",
                schema: "app",
                table: "standings",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_standings_team_id",
                schema: "app",
                table: "standings",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_sync_states_resource",
                schema: "app",
                table: "sync_states",
                column: "resource",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_teams_ext_id",
                schema: "app",
                table: "teams",
                column: "ext_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fixture_events",
                schema: "app");

            migrationBuilder.DropTable(
                name: "fixture_lineup_players",
                schema: "app");

            migrationBuilder.DropTable(
                name: "fixture_player_stats",
                schema: "app");

            migrationBuilder.DropTable(
                name: "player_season_stats",
                schema: "app");

            migrationBuilder.DropTable(
                name: "standings",
                schema: "app");

            migrationBuilder.DropTable(
                name: "sync_states",
                schema: "app");

            migrationBuilder.DropTable(
                name: "fixture_lineups",
                schema: "app");

            migrationBuilder.DropTable(
                name: "players",
                schema: "app");

            migrationBuilder.DropTable(
                name: "fixtures",
                schema: "app");

            migrationBuilder.DropTable(
                name: "competitions",
                schema: "app");

            migrationBuilder.DropTable(
                name: "seasons",
                schema: "app");

            migrationBuilder.DropTable(
                name: "teams",
                schema: "app");
        }
    }
}
