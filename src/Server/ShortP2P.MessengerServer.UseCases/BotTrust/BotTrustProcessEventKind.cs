namespace ShortP2P.MessengerServer.UseCases.BotTrust;

public enum BotTrustProcessEventKind
{
    Info = 0,
    FraudRejected = 1,
    ClaimFlood = 2,
    RatingDecreased = 3,
    RatingRestored = 4,
    RatingIncreased = 5
}
