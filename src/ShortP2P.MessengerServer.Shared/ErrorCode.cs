namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Outcome of a bot client-interaction acknowledgement.</summary>
public enum ErrorCode
{
    /// <summary>The request succeeded.</summary>
    Success = 0,

    /// <summary>Internal error.</summary>
    InternalError = 1,

    /// <summary>Wrong input, or the destination bot was not found.</summary>
    WrongInput = 2
}
