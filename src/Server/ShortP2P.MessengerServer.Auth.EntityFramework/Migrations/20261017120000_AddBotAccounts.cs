using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortP2P.MessengerServer.Auth.EntityFramework.Migrations;

[DbContext(typeof(AuthDbContext))]
[Migration("20261017120000_AddBotAccounts")]
public partial class AddBotAccounts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "bot_accounts",
            columns: table => new
            {
                NetworkId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                BotName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                BotReadableName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                BotDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                KeySalt = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                KeyHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bot_accounts", x => x.NetworkId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bot_accounts_BotName",
            table: "bot_accounts",
            column: "BotName",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "bot_accounts");
    }
}
