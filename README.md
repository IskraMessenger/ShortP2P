# ShortP2P

**ShortP2P** is a prototype **mesh messenger**: an app for exchanging messages between users without a central chat server, with delivery attempts through **multiple nodes** (limited network search depth), **direct UDP**, optional **Bluetooth (RFCOMM on Windows)**, and **relaying** along a known or discovered route.

This is research/demo code: protocols and security parameters are not production-grade. The goal is to explore routing, multi-transport, and end-to-end encryption in one solution.

## What the prototype already includes

- **Local storage** for chats and messages (SQLite via `sqlite-net-pcl`), local **registration/login** (password hash, user keys).
- **Peer discovery** on the LAN over UDP (`ShortP2P.Discovery`).
- **Per-user shared gateway** (`SharedUserUdpGateway`): one UDP socket for data, a separate **presence/ping** channel (port **17501**, broadcast), periodic announcements and peer “visibility” expiry (~10 s interval, ~25 s timeout).
- **Graph-style routing**: configurable **search depth of 1–3 hops** (`P2pRoutingSettings.MaxSearchHops`), waiting for search replies, retries on send failure. A route can be **direct UDP** or a **relay chain** (`ChatRelayRoute`: first hop + strip relay).
- **Adaptive chat transport** (`AdaptiveChatTransportLayer`): if the peer’s last address from ping is known — send there when the transport is available; otherwise — send via the gateway along the current route (including relay).
- **Two UIs**: **WinForms** (Windows) and **.NET MAUI** (repo targets Windows; other platforms — see comments in the `.csproj`). Shared logic lives in **`ShortP2P.Client`**.
- The **`ShortP2P.UI`** project is a stub for a separate UI layer; the main shells are WinForms and MAUI.

## Tech stack

| Area | Implementation |
|------|----------------|
| Platform | .NET 10 (client), Server — **.NET 8**; Crypto — `net8.0` + `net10.0` |
| Transport | `ITransport` abstraction → UDP (`ShortP2P.Transport`), Bluetooth **WinRT RFCOMM** (`ShortP2P.Transport.Bluetooth.Windows`), optional **serial/IR** (`InfraredSerialTransport`) |
| Cryptography | RSA (handshake), session on **ECDH**-like keys; **AES-128-CBC** + **HMAC-SHA256** (truncated tag), upper bound of **128 bytes** for an encrypted packet; large payloads are split in **`MessengerService`** |
| Messenger | `ShortP2P.Messenger`: encryption/fragmentation on top of `ITransport` + `P2PSession` |
| Client | QR (peer exchange), ZXing/ImageSharp, Polly for resilience, LAN scanning, on-disk routing settings |
| Tests | xUnit: `ShortP2P.Crypto.Tests` |

## Repository layout (logical layers)

```
ShortP2P.Crypto/              # Keys, handshake, P2PSession
src/
  ShortP2P.Transport.Abstractions/
  ShortP2P.Transport/         # UDP, BT stub, serial
  ShortP2P.Transport.Bluetooth.Windows/
  ShortP2P.Discovery/         # Overlay: UDP discovery (relay is a separate assembly of the same layer, not Client)
  ShortP2P.Messenger/         # Encrypted binary exchange over transport
  ShortP2P.Client/            # User scenarios, DB, QR; wires Messenger and the overlay layer
  ShortP2P.UI/                # UI stub
  ShortP2P.WinForms/          # Windows desktop
  ShortP2P.MauiApp/           # MAUI client
  ShortP2P.Crypto.Tests/
```

Each project has its own **`README.md`** with a short folder and file layout.

## Build and run (brief)

- Solution: `ShortP2P.sln`.
- Tests: `dotnet test` from the repository root (after restoring packages).
- **MAUI** on non-Windows targets may need `dotnet workload restore` and extending `TargetFrameworks` (see `src/ShortP2P.MauiApp/ShortP2P.MauiApp.csproj`).
- For **unpackaged** MAUI on Windows, if you hit runtime environment errors, see comments in `ShortP2P.MauiApp.csproj` (Windows App SDK).

## Prototype limitations

- Mesh logic is **simplified**: there is no full product-grade fault-tolerant mesh; depth and timeouts are constants/settings.
- Cryptography and key sizes (e.g. RSA-1024 in the generation code) are suitable for experiments, not as a reference for an internet threat model.
- Bluetooth is implemented for **Windows**; other OSes need their own `ITransport` implementations.

## Development tools

This project was developed using **Cursor** — a Visual Studio Code–based IDE with built-in AI assistance.
