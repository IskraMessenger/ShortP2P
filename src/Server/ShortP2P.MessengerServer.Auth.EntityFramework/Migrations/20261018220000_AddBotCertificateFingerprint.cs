using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortP2P.MessengerServer.Auth.EntityFramework.Migrations;

[DbContext(typeof(AuthDbContext))]
[Migration("20261018220000_AddBotCertificateFingerprint")]
public partial class AddBotCertificateFingerprint : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CertificateFingerprintSha256",
            table: "bot_accounts",
            type: "TEXT",
            maxLength: 64,
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CertificateFingerprintSha256",
            table: "bot_accounts");
    }
}
