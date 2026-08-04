# 360 LocalShare — Architecture & Project Structure

A LAN-only file sharing, chat, and group-sharing app for Windows. No internet, no central server — every machine on the network is a peer.

## 1. Assumptions I'm making

Two spots in your brief were slightly ambiguous, so here's how I've interpreted them — flag it if I read them wrong:

- **"Files stored according to sender's name"** — I'm reading this as: on the *receiver's* machine, incoming files land in a folder named after whoever sent them. If Kavindu sends Isuru a file, it lands in `Isuru's LocalShare/Received/Kavindu/`.
- **"Public space"** — a folder each user opts into sharing (via a folder picker), exposed read-only to the LAN so anyone can browse and pull files from it at any time, without the owner explicitly "sending" anything.

## 2. Tech stack

| Layer | Choice | Why |
|---|---|---|
| UI framework | WPF (.NET 8) + [WPF-UI](https://github.com/lepoco/wpfui) | WPF's `Grid`/`Star` sizing and layout system is the strongest responsive-layout story on Windows desktop. WPF-UI gives you a Windows 11 Fluent look (NavigationView, Mica backdrop, rounded corners) without hand-rolling a design system. |
| MVVM | CommunityToolkit.Mvvm | Source-generated `[ObservableProperty]` / `[RelayCommand]` — keeps ViewModels short and testable. |
| Peer discovery | UDP multicast/broadcast | Same trick LocalSend/AirDrop use. No server to run or configure; peers just announce themselves. |
| File transfer + Public Space browsing | Embedded ASP.NET Core Kestrel (minimal API), one instance per app | Gives you chunked multipart upload, resumable download via HTTP range headers, and a real HTTP surface for "list/browse a folder" almost for free. |
| Real-time chat | SignalR, self-hosted on the same Kestrel instance | WebSocket transport, typed hubs, automatic reconnect — works great on LAN, no cloud dependency. |
| Local persistence | SQLite (`Microsoft.Data.Sqlite` + Dapper, or EF Core if you prefer migrations) | File-based, zero-install, plenty fast for chat history / profiles / groups / transfer logs. |
| Packaging | Self-contained single-file publish, or MSIX | Simple side-load onto any machine on the LAN, no .NET runtime install required. |

I'd steer away from **WinForms** (fights you hard on responsive layout) and away from **MAUI** for this one — MAUI's Windows desktop story is still maturing, and you'd give up WPF's more mature data-binding/layout engine for a UI-heavy app like this.

## 3. High-level architecture

Every installed copy of 360 LocalShare is a full peer: it runs its own embedded HTTP server, announces itself via UDP, and talks directly to other peers. There's no coordinator machine — the diagram above shows two peers, but the same shape repeats for every machine on the LAN.

## 4. Solution structure

Clean separation between UI, domain, networking, and data so the networking/transfer engine can be unit-tested without spinning up WPF.

```
LocalShare.sln
│
├── src/
│   ├── LocalShare.App/                    WPF UI project
│   │   ├── Views/
│   │   │   ├── ShellView.xaml             Root window: nav rail + content frame
│   │   │   ├── PeersView.xaml             Live list of discovered LAN peers
│   │   │   ├── ChatView.xaml              1:1 / group chat, drag-drop file zone
│   │   │   ├── GroupsView.xaml            Group create/manage
│   │   │   ├── PublicSpaceView.xaml       Browse a peer's shared folder
│   │   │   ├── TransfersView.xaml         Active/queued/completed transfers
│   │   │   └── ProfileSettingsView.xaml   Display name, avatar, accent color, folders
│   │   ├── ViewModels/                    One VM per View above
│   │   ├── Controls/                      Reusable controls (AvatarBadge, FileChip, ProgressPill)
│   │   ├── Converters/
│   │   ├── Behaviors/                     Adaptive-layout behavior, drag-drop behavior
│   │   ├── Themes/                        Light/dark resource dictionaries
│   │   ├── Assets/
│   │   ├── App.xaml / App.xaml.cs         DI container bootstrap
│   │   └── LocalShare.App.csproj
│   │
│   ├── LocalShare.Core/                   Pure domain layer — no UI or network types
│   │   ├── Models/                        Peer, Profile, Message, Group, TransferItem, PublicShareEntry
│   │   ├── Interfaces/                    IDiscoveryService, ITransferService, IChatService, IRepository<T>
│   │   └── LocalShare.Core.csproj
│   │
│   ├── LocalShare.Networking/              All LAN networking lives here
│   │   ├── Discovery/                      UdpBeaconService, PeerRegistry
│   │   ├── Transfer/                       ChunkedUploadHandler, ResumableDownloadHandler, ChecksumVerifier
│   │   ├── Chat/                           ChatHub (SignalR), GroupFanoutSender
│   │   ├── Http/                           Kestrel host setup, minimal API endpoint definitions
│   │   └── LocalShare.Networking.csproj
│   │
│   ├── LocalShare.Data/                    Persistence
│   │   ├── Entities/
│   │   ├── Repositories/                   ProfileRepository, MessageRepository, GroupRepository, TransferLogRepository
│   │   ├── Migrations/
│   │   └── LocalShare.Data.csproj
│   │
│   └── LocalShare.Common/                  Logging, extension methods, result types
│       └── LocalShare.Common.csproj
│
├── tests/
│   ├── LocalShare.Core.Tests/
│   ├── LocalShare.Networking.Tests/        Fake sockets, protocol round-trip tests
│   └── LocalShare.Data.Tests/
│
├── docs/
└── LocalShare.sln
```

`LocalShare.App` references `Core`, `Networking`, and `Data`. `Networking` and `Data` both reference `Core` only — never each other, and neither references `App`. That keeps the transfer/chat engine fully testable headless, which matters a lot once you're debugging multi-peer transfer bugs.

## 5. Networking protocol

### 5.1 Discovery (UDP)

Each instance broadcasts a small JSON announce packet every few seconds on a fixed multicast group/port (e.g. `239.255.10.10:53210`), and listens for the same from others:

```json
{
  "deviceId": "b3f1...-guid",
  "displayName": "Isuru",
  "avatarHash": "a94e...",
  "accentColor": "#7F77DD",
  "ip": "192.168.1.42",
  "httpPort": 53211,
  "hasPublicSpace": true
}
```

`PeerRegistry` keeps a live `ObservableCollection<Peer>` bound straight to `PeersView` — a peer that stops announcing for N seconds (e.g. 15s) gets marked offline, then removed after a longer grace period.

### 5.2 HTTP API surface (per peer, served by that peer's embedded Kestrel)

| Endpoint | Method | Purpose |
|---|---|---|
| `/api/profile` | GET | This peer's public profile info |
| `/api/transfer/initiate` | POST | Sender tells receiver "incoming file(s), here's the manifest" |
| `/api/transfer/{id}/chunk` | POST | Upload a chunk (multipart) |
| `/api/transfer/{id}/status` | GET | Progress / resume offset |
| `/api/public/list` | GET | List files in this peer's Public Space |
| `/api/public/download/{fileId}` | GET | Pull a file from Public Space (supports `Range` header for resume) |
| `/hub/chat` | WS (SignalR) | Chat messages + typing indicators + group fan-out |

### 5.3 Chat

`ChatHub` handles 1:1 messages directly. For groups, since there's no server to relay through, the sender's `GroupFanoutSender` opens a hub connection to every online group member and sends the same message — simplest thing that works on a flat LAN mesh. File attachments dropped into a chat go through the same transfer engine as a direct send, just tagged with a `chatMessageId` so the UI can render it inline as a file bubble.

## 6. Data model (SQLite)

```
Profile        (Id, DisplayName, AvatarPath, AccentColor, PublicSpacePath, ReceivedFilesRoot)
Peer           (DeviceId, DisplayName, LastSeenAt, LastKnownIp)          -- cache, rebuilt from discovery
Message        (Id, ConversationId, SenderDeviceId, Body, FileTransferId, SentAt, DeliveredAt)
Conversation   (Id, Type[Direct|Group], DisplayName)
Group          (Id, Name, CreatedByDeviceId, CreatedAt)
GroupMember    (GroupId, DeviceId)
TransferLog    (Id, Direction[In|Out], PeerDeviceId, FileName, SizeBytes, Sha256, Status, StartedAt, CompletedAt)
```

## 7. File storage conventions

```
%LOCALAPPDATA%\360LocalShare\
├── Profile\                          avatar, settings.json
├── Received\
│   ├── Kavindu\                      sender's display name (+ short deviceId suffix if it collides)
│   │   └── 2026-07-28_report.pdf
│   └── Nadeesha\
├── ChatAttachments\<conversationId>\ files dropped in chat, mirrors Received but keyed by conversation
└── localshare.db                     SQLite file
```

Because two peers could pick the same display name, disambiguate folder names with a short suffix from `deviceId` whenever a collision is detected (`Kavindu`, `Kavindu (2)`).

**Public Space** is *not* copied anywhere — it stays exactly where the user pointed the folder picker. `LocalShare.Networking` just indexes it (via `FileSystemWatcher` so the listing stays live) and serves it read-only over `/api/public/*`. Nothing about "receiving" is involved; peers pull from it whenever they want.

## 8. Group sharing model

A group is just a locally-defined membership list (name + set of `deviceId`s), created by whoever starts it and pushed to the other members as a small "you've been added to a group" message the first time. There's no group owner enforcing membership after that — any member can add/remove others, and each member's app keeps its own copy of the roster. Keep this v1-simple; a "moderator" concept can come later if you need it.

## 9. Responsive UI/UX approach

- **Shell**: a WPF-UI `NavigationView` as the root — it already collapses its side pane to icons-only below a width threshold, which gets you 80% of "adapts to screen size" for free.
- **Content panels**: build every view's root layout with `Grid` + `*`/`Auto` sizing, never fixed pixel widths. A three-pane view (peer list / chat / details) should use `ColumnDefinition Width="*"`, `"2*"`, `"Auto"` so panes resize proportionally instead of clipping.
- **Breakpoints**: raw WPF doesn't have WinUI's `AdaptiveTrigger`, so implement a small attached behavior that watches the window's `SizeChanged` and swaps a `VisualState` (or just toggles a panel's `Visibility`/column width) at your chosen breakpoints — e.g. hide the details pane below 900px, collapse the nav rail below 700px.

```xml
<Grid>
  <Grid.ColumnDefinitions>
    <ColumnDefinition x:Name="PeerListColumn" Width="260"/>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition x:Name="DetailsColumn" Width="2*"/>
  </Grid.ColumnDefinitions>
  <!-- behaviors:AdaptiveLayoutBehavior collapses DetailsColumn to 0
       and PeerListColumn to 72 (icon rail) below defined breakpoints -->
</Grid>
```

- **Minimum window size**: set `MinWidth`/`MinHeight` on the shell so panels never get squeezed into unusable slivers — better to scroll than to mangle a layout.

## 10. Suggested build order

1. **Phase 1** — Profile setup, UDP discovery, direct one-to-one file send (no chat yet). This alone gets you to LocalSend parity.
2. **Phase 2** — Chat (1:1), with file drop-in-chat reusing the Phase 1 transfer engine.
3. **Phase 3** — Public Space (folder picker + browse/pull over HTTP).
4. **Phase 4** — Groups (chat fan-out + group file share).
5. **Phase 5** — Polish: transfer resume, notifications, drag-and-drop from Explorer, themes, MSIX packaging.

## 11. Key NuGet packages

- `CommunityToolkit.Mvvm`
- `WPF-UI` (Fluent design system for WPF)
- `Microsoft.AspNetCore.*` (self-hosted Kestrel + minimal APIs, referenced from a WPF app via the `Microsoft.NET.Sdk.Web` style hosting)
- `Microsoft.AspNetCore.SignalR.Client` and server-side SignalR (bundled with ASP.NET Core)
- `Microsoft.Data.Sqlite` (+ `Dapper`, or `Microsoft.EntityFrameworkCore.Sqlite`)
- `System.Net.NetworkInformation` (built-in, for enumerating local NICs/subnets)

## 12. Software updates

Split into two parts: how updates get built and applied to a machine, and how the update file actually reaches machines that might not have internet — the second one's slightly unusual here given the "no internet needed" premise.

### 12.1 Update engine: Velopack

Recommended over ClickOnce or a hand-rolled updater — [Velopack](https://velopack.io) is the actively maintained successor to Squirrel.Windows, purpose-built for this. It applies updates in seconds without UAC prompts, its CLI (`vpk`) generates the installer, delta update packages, and a self-updating portable package from a build in one command, and the update feed can live anywhere — GitHub Releases, S3, a plain file server, or a folder on your own LAN.

Fit for 360 LocalShare specifically:
- Delta patching means a client going from v1.2.0 → v1.2.1 downloads a small diff, not the whole app again.
- Feed hosting flexibility means you're not locked into requiring internet for the check itself.

### 12.2 Wiring it into the architecture

- Add an `appVersion` field to the UDP discovery announce packet already being broadcast (alongside `deviceId`, `displayName`, etc.). Costs nothing extra, and lets `PeersView` show an "update available" badge next to any peer running an older build — discoverable purely from LAN traffic, no internet required to know an update exists.
- On startup (and periodically thereafter), call Velopack's `UpdateManager.CheckForUpdatesAsync()` against the feed. If nothing's reachable (offline LAN), it fails silently and the app carries on — this should never block usage.
- Download in the background, apply on next restart — never force a restart mid-transfer.

### 12.3 LAN-relayed updates (offline fallback)

Since the app already has a working P2P file transfer engine, it can lean on that for environments where machines don't have general internet access (locked-down office LANs, air-gapped setups):

1. One peer with internet access checks the Velopack feed and downloads the new installer package.
2. That peer re-shares the downloaded package into its own Public Space.
3. Other machines on the LAN pull it exactly the way they'd pull any other file, then apply it locally.

Optional, but costs nothing extra to support since the plumbing already exists.

### 12.4 Release process

- **Semantic versioning** (`major.minor.patch`) — patch for fixes, minor for new features (e.g. shipping Groups in Phase 4), major for breaking protocol changes.
- **CI**: a GitHub Actions workflow running `dotnet publish`, then `vpk pack`/`vpk upload` on tag push, for fully automated releases.
- **Protocol version guard**: add a separate `protocolVersion` field (distinct from `appVersion`) to the discovery packet. If the transfer/chat wire format ever changes, mismatched clients can detect it and show "update needed to talk to this peer" instead of silently failing a transfer.
- **Rollback safety**: Velopack keeps prior version packages around by default, so a bad release isn't unrecoverable — worth testing a release on one machine before letting the whole LAN auto-update to it.
