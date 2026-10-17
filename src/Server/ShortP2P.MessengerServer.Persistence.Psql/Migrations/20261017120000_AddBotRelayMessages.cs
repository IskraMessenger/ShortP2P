using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortP2P.MessengerServer.Persistence.Psql.Migrations;

[DbContext(typeof(MessengerDbContext))]
[Migration("20261017120000_AddBotRelayMessages")]
public partial class AddBotRelayMessages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "bot_to_client_messages",
            columns: table => new
            {
                MessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                BotNetworkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ClientNetworkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EncryptedMessageBase64 = table.Column<string>(type: "text", nullable: false),
                CorrelationId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bot_to_client_messages", x => x.MessageId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bot_to_client_messages_BotNetworkId",
            table: "bot_to_client_messages",
            column: "BotNetworkId");

        migrationBuilder.CreateIndex(
            name: "IX_bot_to_client_messages_ClientNetworkId",
            table: "bot_to_client_messages",
            column: "ClientNetworkId");

        migrationBuilder.CreateIndex(
            name: "IX_bot_to_client_messages_CreatedUtc",
            table: "bot_to_client_messages",
            column: "CreatedUtc");

        migrationBuilder.CreateTable(
            name: "client_to_bot_messages",
            columns: table => new
            {
                MessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ClientNetworkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                BotNetworkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EncryptedMessageBase64 = table.Column<string>(type: "text", nullable: false),
                CorrelationId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_client_to_bot_messages", x => x.MessageId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_client_to_bot_messages_BotNetworkId",
            table: "client_to_bot_messages",
            column: "BotNetworkId");

        migrationBuilder.CreateIndex(
            name: "IX_client_to_bot_messages_CreatedUtc",
            table: "client_to_bot_messages",
            column: "CreatedUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "bot_to_client_messages");
        migrationBuilder.DropTable(name: "client_to_bot_messages");
    }
}
