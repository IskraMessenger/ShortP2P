using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShortP2P.MessengerServer.Api.Extensions;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;

namespace ShortP2P.MessengerServer.Api.Controllers;

[ApiController]
[Authorize]
[RequireHttps]
[Route($"{ApiRoutes.Prefix}/bots")]
public sealed class BotsController(
    GetBotBlackListUseCase getBotBlackList,
    AddBotToBlacklistUseCase addBotToBlacklist,
    RemoveBotFromBlacklistUseCase removeBotFromBlacklist) : ControllerBase
{
    /// <summary>List blacklisted bot network ids (authorized clients).</summary>
    [HttpGet("blacklist")]
    public async Task<IActionResult> GetBotBlackListAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = HttpContext.RequireNetworkId();
            var ids = await getBotBlackList.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return Ok(ids.Select(id => new BotBlacklistEntryDto { NetworkId = id }).ToArray());
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    /// <summary>Add a bot network id to the server blacklist.</summary>
    [HttpPost("blacklist")]
    public async Task<IActionResult> AddToBlacklistAsync(
        [FromBody] AddBotToBlacklistRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = HttpContext.RequireNetworkId();
            await addBotToBlacklist
                .ExecuteAsync(new AddBotToBlacklistCommand(request.NetworkId), cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    /// <summary>Remove a bot network id from the server blacklist.</summary>
    [HttpDelete("blacklist/{networkId}")]
    public async Task<IActionResult> RemoveFromBlacklistAsync(
        string networkId,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = HttpContext.RequireNetworkId();
            await removeBotFromBlacklist
                .ExecuteAsync(new RemoveBotFromBlacklistCommand(networkId), cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }
}
