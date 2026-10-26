using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShortP2P.MessengerServer.Api.Extensions;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.BotClient;

namespace ShortP2P.MessengerServer.Api.Controllers.Bot;

[ApiController]
[Authorize]
[RequireHttps]
[Route($"{ApiRoutes.Prefix}/bot_client_interaction")]
public sealed class BotClientInteractionController(
    WaitForBotMessagesUseCase waitForBotMessages,
    SendMessageToBotUseCase sendMessageToBot,
    AcknowledgeBotMessageUseCase acknowledgeBotMessage) : ControllerBase
{
    /// <summary>Long-poll for encrypted messages from bots to this client.</summary>
    [HttpPost("wait_for_messages")]
    public async Task<IActionResult> WaitForMessages(
        [FromBody] BotWaitForMessagesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var caller = HttpContext.RequireNetworkId();
            if (!string.Equals(caller, request.NetworkId?.Trim(), StringComparison.Ordinal))
                throw UseCaseException.Unauthorized("networkId must match the authenticated client.");

            var messages = await waitForBotMessages
                .ExecuteAsync(new WaitForBotMessagesQuery(caller, request.TimeoutSeconds), cancellationToken)
                .ConfigureAwait(false);

            return Ok(new BotWaitForMessagesResponse
            {
                Messages = messages.Select(m => new BotClientInboundMessageDto
                {
                    MessageId = m.MessageId,
                    BotNetworkId = m.BotNetworkId,
                    DateTime = m.CreatedUtc,
                    Message = m.EncryptedMessageBase64,
                    CorrelationId = m.CorrelationId
                }).ToArray()
            });
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }

    /// <summary>Deliver one encrypted message from the client to a bot.</summary>
    [HttpPost("send_message")]
    public async Task<IActionResult> SendMessage(
        [FromBody] BotSendMessageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var caller = HttpContext.RequireNetworkId();
            await sendMessageToBot
                .ExecuteAsync(
                    new SendMessageToBotCommand(
                        caller,
                        request.MessageId,
                        request.NetworkId,
                        request.DateTime,
                        request.Message,
                        request.CorrelationId),
                    cancellationToken)
                .ConfigureAwait(false);

            return Ok(new BotSendMessageResponse
            {
                MessageId = request.MessageId,
                ErrorCode = ErrorCode.Success,
                ErrorMessage = string.Empty
            });
        }
        catch (UseCaseException ex) when (ex.Code is "Validation" or "NotFound")
        {
            return Ok(new BotSendMessageResponse
            {
                MessageId = request.MessageId ?? string.Empty,
                ErrorCode = ErrorCode.WrongInput,
                ErrorMessage = ex.Message
            });
        }
        catch (UseCaseException ex)
        {
            return Ok(new BotSendMessageResponse
            {
                MessageId = request.MessageId,
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = ex.Message
            });
        }
    }

    /// <summary>Delivery receipt. The stored bot → client message is removed.</summary>
    [HttpPost("receipts")]
    public async Task<IActionResult> SubmitReceipt(
        [FromBody] BotMessageReceiptRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var caller = HttpContext.RequireNetworkId();
            await acknowledgeBotMessage
                .ExecuteAsync(new AcknowledgeBotMessageCommand(caller, request.MessageId), cancellationToken)
                .ConfigureAwait(false);

            return Accepted();
        }
        catch (UseCaseException ex)
        {
            return this.ToApiErrorResult(ex);
        }
    }
}
