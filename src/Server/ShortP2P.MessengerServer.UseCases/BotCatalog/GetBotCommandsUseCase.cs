using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotCatalog;

/// <summary>Returns the command list a bot last reported to this server.</summary>
public sealed class GetBotCommandsUseCase(IBotAccountRepository accounts)
{
    public async Task<BotCommandsDto> ExecuteAsync(
        GetBotCommandsQuery query,
        CancellationToken cancellationToken = default)
    {
        var networkId = query.BotNetworkId.Trim();
        var account = await accounts.FindByNetworkIdAsync(networkId, cancellationToken).ConfigureAwait(false);
        if (account is null)
            throw UseCaseException.NotFound("Bot was not found.");

        var commands = account.Commands
            .Select(command => new BotCommandDto
            {
                Command = command.Command,
                Args = command.Args?
                    .Select(arg => new BotCommandArgumentDto
                    {
                        ArgName = arg.ArgName,
                        ArgType = arg.ArgType
                    })
                    .ToArray(),
                Description = command.Description
            })
            .ToArray();

        return new BotCommandsDto
        {
            BotNetworkId = account.NetworkId.Trim(),
            Commands = commands
        };
    }
}
