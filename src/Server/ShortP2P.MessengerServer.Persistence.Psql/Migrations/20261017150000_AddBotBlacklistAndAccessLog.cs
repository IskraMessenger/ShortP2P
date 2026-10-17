using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShortP2P.MessengerServer.Persistence.Psql.Migrations;

[DbContext(typeof(MessengerDbContext))]
[Migration("20261017150000_AddBotBlacklistAndAccessLog")]
public partial class AddBotBlacklistAndAccessLog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "bot_blacklist",
            columns: table => new
            {
                NetworkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                AddedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bot_blacklist", x => x.NetworkId);
            });

        migrationBuilder.CreateTable(
            name: "bot_access_log",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NetworkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                BotName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                Operation = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Detail = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bot_access_log", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bot_access_log_AtUtc",
            table: "bot_access_log",
            column: "AtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_bot_access_log_NetworkId",
            table: "bot_access_log",
            column: "NetworkId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "bot_access_log");
        migrationBuilder.DropTable(name: "bot_blacklist");
    }
}
