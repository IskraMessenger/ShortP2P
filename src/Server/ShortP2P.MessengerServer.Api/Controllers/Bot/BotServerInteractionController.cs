using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.Domain;
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
    WaitForClientMessagesUseCase waitForClientMessages,
    AcknowledgeClientMessageUseCase acknowledgeClientMessage,
    ICryptoKeysRepository cryptoKeys) : ControllerBase
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
                    request.FingerprintSha256,
                    request.MessagePublicKey,
                    request.Version,
                    MapCommands(request.Commands)),
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
                    request.FingerprintSha256,
                    request.Version,
                    MapCommands(request.Commands)),
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
            var botId = request.BotNetworkId.Trim();
            var messages = await waitForClientMessages.ExecuteAsync(
                new WaitForClientMessagesQuery(botId, request.BotKey, request.TimeoutSeconds),
                cancellationToken).ConfigureAwait(false);

            var dtoList = new List<BotClientMessageDto>(messages.Count);
            foreach (var m in messages)
            {
                var clientKey = await cryptoKeys
                    .FindAsync(m.ClientNetworkId, botId, cancellationToken)
                    .ConfigureAwait(false);
                dtoList.Add(new BotClientMessageDto
                {
                    NetworkId = m.ClientNetworkId,
                    EncryptedMessageBase64 = m.EncryptedMessageBase64,
                    CorrelationId = m.CorrelationId,
                    ClientMessagePublicKey = string.IsNullOrWhiteSpace(clientKey?.PublicKey)
                        ? null
                        : clientKey.PublicKey.Trim()
                });
            }

            return Ok(new BotWaitForIncomeMessagesResponse
            {
                RequestId = request.RequestId.Trim(),
                Messages = dtoList
            });
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    /// <summary>Bot receipt. Removes the client → bot message and its ciphertext chunks.</summary>
    [HttpPost("receipts")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitReceipt(
        [FromBody] BotServerReceiptRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await acknowledgeClientMessage
                .ExecuteAsync(
                    new AcknowledgeClientMessageCommand(request.BotNetworkId, request.BotKey, request.MessageId),
                    cancellationToken)
                .ConfigureAwait(false);

            return Accepted();
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    private static IReadOnlyList<BotCommand>? MapCommands(IReadOnlyList<BotCommandDto>? commands) =>
        commands?.Select(command => new BotCommand
        {
            Command = command.Command,
            Args = command.Args?.Select(arg => new BotCommandArgument
            {
                ArgName = arg.ArgName,
                ArgType = arg.ArgType
            }).ToArray(),
            Description = command.Description
        }).ToArray();
}
