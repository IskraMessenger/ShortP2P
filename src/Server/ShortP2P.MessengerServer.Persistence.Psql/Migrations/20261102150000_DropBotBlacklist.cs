using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortP2P.MessengerServer.Persistence.Psql.Migrations;

[DbContext(typeof(MessengerDbContext))]
[Migration("20261102150000_DropBotBlacklist")]
public partial class DropBotBlacklist : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "bot_blacklist");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
