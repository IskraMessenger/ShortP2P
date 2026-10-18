using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.BotServer;

namespace ShortP2P.MessengerServer.Api.Controllers.Bot;

[ApiController]
[RequireHttps]
[Route($"{ApiRoutes.Prefix}/bot_server_interaction")]
public sealed class BotServerInteractionController(
    RegisterBotUseCase registerBot,
    LoginBotUseCase loginBot,
    RemoveBotUseCase removeBot,
    DeliverBotMessagesUseCase deliverBotMessages,
    WaitForClientMessagesUseCase waitForClientMessages) : ControllerBase
{
    /// <summary>Register a bot. The bot key is returned once.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] BotRegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await registerBot.ExecuteAsync(
                new RegisterBotCommand(
                    request.NetworkId,
                    request.BotName,
                    request.BotReadableName,
                    request.BotDescription,
                    request.FingerprintSha256),
                cancellationToken).ConfigureAwait(false);

            return StatusCode(StatusCodes.Status201Created, new BotRegisterResponse
            {
                NetworkId = result.NetworkId,
                BotName = result.BotName,
                BotReadableName = result.BotReadableName,
                BotDescription = result.BotDescription,
                BotKey = result.BotKey,
                ServerFingerprintSha256 = result.ServerFingerprintSha256
            });
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    /// <summary>Authorize a bot with its bot key.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] BotLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await loginBot.ExecuteAsync(
                new LoginBotCommand(
                    request.NetworkId,
                    request.BotName,
                    request.BotKey,
                    request.FingerprintSha256),
                cancellationToken).ConfigureAwait(false);

            return Ok(new BotLoginResponse
            {
                Token = result.Token,
                ExpiresAtUtc = result.ExpiresAtUtc,
                ServerFingerprintSha256 = result.ServerFingerprintSha256
            });
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    /// <summary>Remove a bot and its queued relay messages.</summary>
    [HttpPost("remove")]
    [AllowAnonymous]
    public async Task<IActionResult> Remove(
        [FromBody] BotRemoveRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await removeBot
                .ExecuteAsync(new RemoveBotCommand(request.NetworkId, request.BotKey), cancellationToken)
                .ConfigureAwait(false);

            return Ok(new BotRemoveResponse
            {
                NetworkId = request.NetworkId,
                BotKey = request.BotKey
            });
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    /// <summary>Store encrypted bot → client messages until each client receipt.</summary>
    [HttpPost("send_messages")]
    [AllowAnonymous]
    public async Task<IActionResult> SendMessages(
        [FromBody] BotSendMessagesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var messages = request.Messages
                .Select(m => new DeliverBotMessageItem(m.NetworkId, m.EncryptedMessageBase64, m.CorrelationId))
                .ToArray();

            await deliverBotMessages.ExecuteAsync(
                new DeliverBotMessagesCommand(request.BotNetworkId, request.BotKey, messages),
                cancellationToken).ConfigureAwait(false);

            return Ok(new BotSendMessagesResponse { RequestId = request.RequestId.Trim() });
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    /// <summary>Long-poll client → bot messages. Returned messages leave the queue.</summary>
    [HttpPost("wait_for_income_messages")]
    [AllowAnonymous]
    public async Task<IActionResult> WaitForIncomeMessages(
        [FromBody] BotWaitForIncomeMessagesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var messages = await waitForClientMessages.ExecuteAsync(
                new WaitForClientMessagesQuery(request.BotNetworkId, request.BotKey, request.TimeoutSeconds),
                cancellationToken).ConfigureAwait(false);

            return Ok(new BotWaitForIncomeMessagesResponse
            {
                RequestId = request.RequestId.Trim(),
                Messages = messages.Select(m => new BotClientMessageDto
                {
                    NetworkId = m.ClientNetworkId,
                    EncryptedMessageBase64 = m.EncryptedMessageBase64,
                    CorrelationId = m.CorrelationId
                }).ToArray()
            });
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }
}
