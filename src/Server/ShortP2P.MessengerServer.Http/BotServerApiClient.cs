using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Http;

/// <summary>
/// HTTPS client for bot ↔ messenger-server (<c>/bot_server_interaction</c>).
/// Auth is via <c>botKey</c> in the body (no Bearer session).
/// Bots present their TLS certificate fingerprint on register/login (SHO-18).
/// </summary>
public sealed class BotServerApiClient(HttpClient httpClient) : IBotServerApi
{
    public async Task<BotRegisterResponse> RegisterAsync(
        BotRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        Require.NotNull(request);

        using var response = await httpClient
            .PostAsync(ApiRoutes.BotServerRegister, MessengerServerJson.ToJsonContent(request), cancellationToken)
            .ConfigureAwait(false);

        return await MessengerServerJson
            .ReadJsonAsync<BotRegisterResponse>(response, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<BotLoginResponse> LoginAsync(
        BotLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        Require.NotNull(request);

        using var response = await httpClient
            .PostAsync(ApiRoutes.BotServerLogin, MessengerServerJson.ToJsonContent(request), cancellationToken)
            .ConfigureAwait(false);

        return await MessengerServerJson
            .ReadJsonAsync<BotLoginResponse>(response, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<BotRemoveResponse> RemoveAsync(
        BotRemoveRequest request,
        CancellationToken cancellationToken = default)
    {
        Require.NotNull(request);

        using var response = await httpClient
            .PostAsync(ApiRoutes.BotServerRemove, MessengerServerJson.ToJsonContent(request), cancellationToken)
            .ConfigureAwait(false);

        return await MessengerServerJson
            .ReadJsonAsync<BotRemoveResponse>(response, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<BotSendMessagesResponse> SendMessagesAsync(
        BotSendMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        Require.NotNull(request);

        using var response = await httpClient
            .PostAsync(ApiRoutes.BotServerSendMessages, MessengerServerJson.ToJsonContent(request), cancellationToken)
            .ConfigureAwait(false);

        return await MessengerServerJson
            .ReadJsonAsync<BotSendMessagesResponse>(response, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<BotWaitForIncomeMessagesResponse> WaitForIncomeMessagesAsync(
        BotWaitForIncomeMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        Require.NotNull(request);

        using var response = await httpClient
            .PostAsync(
                ApiRoutes.BotServerWaitForIncomeMessages,
                MessengerServerJson.ToJsonContent(request),
                cancellationToken)
            .ConfigureAwait(false);

        return await MessengerServerJson
            .ReadJsonAsync<BotWaitForIncomeMessagesResponse>(response, cancellationToken)
            .ConfigureAwait(false);
    }
}
