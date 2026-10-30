using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortP2P.MessengerServer.Auth.EntityFramework.Migrations;

[DbContext(typeof(AuthDbContext))]
[Migration("20261030190000_AddBotCommands")]
public partial class AddBotCommands : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CommandsJson",
            table: "bot_accounts",
            type: "TEXT",
            maxLength: 32768,
            nullable: false,
            defaultValue: "[]");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CommandsJson",
            table: "bot_accounts");
    }
}
