namespace ShortP2P.MessengerServer.UseCases.BotBlacklist;

/// <summary>Stable operation names stored in <c>bot_access_log</c>.</summary>
public static class BotAccessOperations
{
    public const string Register = "register";
    public const string Login = "login";
    public const string Remove = "remove";
    public const string SendMessages = "send_messages";
    public const string WaitForIncomeMessages = "wait_for_income_messages";
}
