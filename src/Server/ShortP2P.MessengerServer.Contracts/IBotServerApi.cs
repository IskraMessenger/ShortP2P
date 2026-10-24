using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Contracts;

/// <summary>
/// Bot ↔ messenger-server HTTPS operations (<c>/bot_server_interaction</c>).
/// Client implementation: <c>ShortP2P.MessengerServer.Http.BotServerApiClient</c>.
/// </summary>
public interface IBotServerApi
{
    /// <summary>POST <see cref="ApiRoutes.BotServerRegister"/> — register bot; returns botKey once.</summary>
    Task<BotRegisterResponse> RegisterAsync(
        BotRegisterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>POST <see cref="ApiRoutes.BotServerLogin"/> — authorize by botKey + certificate fingerprint.</summary>
    Task<BotLoginResponse> LoginAsync(
        BotLoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>POST <see cref="ApiRoutes.BotServerRemove"/> — remove bot and queued relay messages.</summary>
    Task<BotRemoveResponse> RemoveAsync(
        BotRemoveRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>POST <see cref="ApiRoutes.BotServerSendMessages"/> — store encrypted bot → client messages.</summary>
    Task<BotSendMessagesResponse> SendMessagesAsync(
        BotSendMessagesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// POST <see cref="ApiRoutes.BotServerWaitForIncomeMessages"/> — long-poll client → bot messages.
    /// </summary>
    Task<BotWaitForIncomeMessagesResponse> WaitForIncomeMessagesAsync(
        BotWaitForIncomeMessagesRequest request,
        CancellationToken cancellationToken = default);
}
