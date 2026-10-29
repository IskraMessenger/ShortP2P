using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShortP2P.MessengerServer.Persistence.Psql.Migrations;

[DbContext(typeof(MessengerDbContext))]
[Migration("20261028120000_AddBlobChunks")]
public partial class AddBlobChunks : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "blob_chunks",
            columns: table => new
            {
                BlobId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                SrcNetworkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                TgtNetworkId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                TotalChunks = table.Column<int>(type: "integer", nullable: false),
                TotalBytes = table.Column<long>(type: "bigint", nullable: false),
                Ciphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_blob_chunks", x => new { x.BlobId, x.ChunkIndex });
            });

        migrationBuilder.CreateIndex(
            name: "IX_blob_chunks_CreatedUtc",
            table: "blob_chunks",
            column: "CreatedUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "blob_chunks");
    }
}
