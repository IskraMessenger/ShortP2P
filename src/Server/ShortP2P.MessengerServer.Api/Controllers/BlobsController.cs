using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShortP2P.MessengerServer.Api.Extensions;
using ShortP2P.MessengerServer.Api.Validation;
using ShortP2P.MessengerServer.Api.Validation.Models;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.Blobs;

namespace ShortP2P.MessengerServer.Api.Controllers;

[ApiController]
[Authorize]
[Route($"{ApiRoutes.Prefix}/blobs")]
public sealed class BlobsController(
    PutBlobUseCase putBlobUseCase,
    GetBlobUseCase getBlobUseCase,
    DeleteBlobUseCase deleteBlobUseCase,
    PutBlobChunkUseCase putBlobChunkUseCase,
    GetBlobChunkUseCase getBlobChunkUseCase,
    GetBlobChunkManifestUseCase getBlobChunkManifestUseCase) : ControllerBase
{
    [HttpPut("{blobId}")]
    [Consumes("application/octet-stream")]
    [RequestSizeLimit(BlobLimits.MaxCiphertextBytes)]
    public async Task<IActionResult> PutBlobAsync(
        string blobId,
        [FromQuery] string? targetNetworkId,
        [FromHeader(Name = BlobLimits.TargetNetworkIdHeader)] string? targetNetworkIdHeader,
        CancellationToken cancellationToken)
    {
        try
        {
            await ValidationGate
                .EnsureValidAsync(HttpContext.RequestServices, new BlobIdArgument(blobId), cancellationToken)
                .ConfigureAwait(false);

            var caller = HttpContext.RequireNetworkId();
            var tgt = !string.IsNullOrWhiteSpace(targetNetworkId)
                ? targetNetworkId.Trim()
                : targetNetworkIdHeader?.Trim() ?? "";

            await ValidationGate
                .EnsureValidAsync(HttpContext.RequestServices, new TargetNetworkIdArgument(tgt), cancellationToken)
                .ConfigureAwait(false);

            using var buffer = new MemoryStream();
            await Request.Body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            var ciphertext = buffer.ToArray();

            await putBlobUseCase
                .ExecuteAsync(new PutBlobCommand(blobId.Trim(), caller, tgt, ciphertext), cancellationToken)
                .ConfigureAwait(false);

            return Accepted();
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    [HttpGet("{blobId}")]
    public async Task<IActionResult> GetBlobAsync(string blobId, CancellationToken cancellationToken)
    {
        try
        {
            await ValidationGate
                .EnsureValidAsync(HttpContext.RequestServices, new BlobIdArgument(blobId), cancellationToken)
                .ConfigureAwait(false);

            var caller = HttpContext.RequireNetworkId();
            var blob = await getBlobUseCase
                .ExecuteAsync(new GetBlobQuery(blobId.Trim(), caller), cancellationToken)
                .ConfigureAwait(false);

            return File(blob.Ciphertext, "application/octet-stream");
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    [HttpDelete("{blobId}")]
    public async Task<IActionResult> DeleteBlobAsync(string blobId, CancellationToken cancellationToken)
    {
        try
        {
            await ValidationGate
                .EnsureValidAsync(HttpContext.RequestServices, new BlobIdArgument(blobId), cancellationToken)
                .ConfigureAwait(false);

            var caller = HttpContext.RequireNetworkId();
            await deleteBlobUseCase
                .ExecuteAsync(new DeleteBlobCommand(blobId.Trim(), caller), cancellationToken)
                .ConfigureAwait(false);

            return NoContent();
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    [HttpPut("{blobId}/chunks/{chunkIndex:int}")]
    [Consumes("application/octet-stream")]
    [RequestSizeLimit(BlobLimits.ChunkSizeBytes)]
    public async Task<IActionResult> PutChunkAsync(
        string blobId,
        int chunkIndex,
        [FromQuery] string? targetNetworkId,
        [FromQuery] int totalChunks,
        [FromQuery] long totalBytes,
        [FromHeader(Name = BlobLimits.TargetNetworkIdHeader)] string? targetNetworkIdHeader,
        CancellationToken cancellationToken)
    {
        try
        {
            await ValidationGate
                .EnsureValidAsync(HttpContext.RequestServices, new BlobIdArgument(blobId), cancellationToken)
                .ConfigureAwait(false);

            var caller = HttpContext.RequireNetworkId();
            var tgt = !string.IsNullOrWhiteSpace(targetNetworkId)
                ? targetNetworkId.Trim()
                : targetNetworkIdHeader?.Trim() ?? "";

            await ValidationGate
                .EnsureValidAsync(HttpContext.RequestServices, new TargetNetworkIdArgument(tgt), cancellationToken)
                .ConfigureAwait(false);

            using var buffer = new MemoryStream();
            await Request.Body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

            await putBlobChunkUseCase
                .ExecuteAsync(
                    new PutBlobChunkCommand(
                        blobId.Trim(),
                        chunkIndex,
                        caller,
                        tgt,
                        totalChunks,
                        totalBytes,
                        buffer.ToArray()),
                    cancellationToken)
                .ConfigureAwait(false);

            return Accepted();
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    [HttpGet("{blobId}/chunks")]
    public async Task<IActionResult> GetChunkManifestAsync(string blobId, CancellationToken cancellationToken)
    {
        try
        {
            await ValidationGate
                .EnsureValidAsync(HttpContext.RequestServices, new BlobIdArgument(blobId), cancellationToken)
                .ConfigureAwait(false);

            var caller = HttpContext.RequireNetworkId();
            var manifest = await getBlobChunkManifestUseCase
                .ExecuteAsync(new GetBlobChunkManifestQuery(blobId.Trim(), caller), cancellationToken)
                .ConfigureAwait(false);

            return Ok(new BlobChunkManifestDto
            {
                BlobId = manifest.BlobId,
                TotalChunks = manifest.TotalChunks,
                TotalBytes = manifest.TotalBytes,
                Indexes = manifest.Indexes.ToArray()
            });
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    [HttpGet("{blobId}/chunks/{chunkIndex:int}")]
    public async Task<IActionResult> GetChunkAsync(
        string blobId,
        int chunkIndex,
        CancellationToken cancellationToken)
    {
        try
        {
            await ValidationGate
                .EnsureValidAsync(HttpContext.RequestServices, new BlobIdArgument(blobId), cancellationToken)
                .ConfigureAwait(false);

            var caller = HttpContext.RequireNetworkId();
            var chunk = await getBlobChunkUseCase
                .ExecuteAsync(new GetBlobChunkQuery(blobId.Trim(), chunkIndex, caller), cancellationToken)
                .ConfigureAwait(false);

            return File(chunk.Ciphertext, "application/octet-stream");
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }
}
