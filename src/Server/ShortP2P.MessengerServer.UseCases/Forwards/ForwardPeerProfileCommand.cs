using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.Forwards;

public sealed record ForwardPeerProfileCommand(
    string ForwardId,
    string SrcNetworkId,
    string TgtNetworkId,
    ForwardKind Kind,
    string PayloadBase64,
    string ServerOrigin);
