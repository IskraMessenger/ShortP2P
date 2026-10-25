using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortP2P.MessengerServer.Auth.EntityFramework.Migrations;

[DbContext(typeof(AuthDbContext))]
[Migration("20261025180000_AddBotMessagePublicKey")]
public partial class AddBotMessagePublicKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MessagePublicKey",
            table: "bot_accounts",
            type: "TEXT",
            maxLength: 16384,
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MessagePublicKey",
            table: "bot_accounts");
    }
}
