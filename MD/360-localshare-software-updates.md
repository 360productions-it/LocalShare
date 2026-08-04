# 360 LocalShare — Managing software updates

Split into two parts: how updates get built and applied to a machine, and how the update file actually reaches machines that might not have internet — the second one's slightly unusual here given the "no internet needed" premise.

## 1. Update engine: Velopack

Recommended over ClickOnce or a hand-rolled updater — [Velopack](https://velopack.io) is the actively maintained successor to Squirrel.Windows, purpose-built for this. It applies updates in seconds without UAC prompts, its CLI (`vpk`) generates the installer, delta update packages, and a self-updating portable package from a build in one command, and the update feed can live anywhere — GitHub Releases, S3, a plain file server, or a folder on your own LAN.

Fit for 360 LocalShare specifically:
- Delta patching means a client going from v1.2.0 → v1.2.1 downloads a small diff, not the whole app again.
- Feed hosting flexibility means you're not locked into requiring internet for the check itself.

## 2. Wiring it into the architecture

- Add an `appVersion` field to the UDP discovery announce packet already being broadcast (alongside `deviceId`, `displayName`, etc.). Costs nothing extra, and lets `PeersView` show an "update available" badge next to any peer running an older build — discoverable purely from LAN traffic, no internet required to know an update exists.
- On startup (and periodically thereafter), call Velopack's `UpdateManager.CheckForUpdatesAsync()` against the feed. If nothing's reachable (offline LAN), it fails silently and the app carries on — this should never block usage.
- Download in the background, apply on next restart — never force a restart mid-transfer.

## 3. LAN-relayed updates (offline fallback)

Since the app already has a working P2P file transfer engine, it can lean on that for environments where machines don't have general internet access (locked-down office LANs, air-gapped setups):

1. One peer with internet access checks the Velopack feed and downloads the new installer package.
2. That peer re-shares the downloaded package into its own Public Space.
3. Other machines on the LAN pull it exactly the way they'd pull any other file, then apply it locally.

Optional, but costs nothing extra to support since the plumbing already exists.

## 4. Release process

- **Semantic versioning** (`major.minor.patch`) — patch for fixes, minor for new features (e.g. shipping Groups in Phase 4), major for breaking protocol changes.
- **CI**: a GitHub Actions workflow running `dotnet publish`, then `vpk pack`/`vpk upload` on tag push, for fully automated releases.
- **Protocol version guard**: add a separate `protocolVersion` field (distinct from `appVersion`) to the discovery packet. If the transfer/chat wire format ever changes, mismatched clients can detect it and show "update needed to talk to this peer" instead of silently failing a transfer.
- **Rollback safety**: Velopack keeps prior version packages around by default, so a bad release isn't unrecoverable — worth testing a release on one machine before letting the whole LAN auto-update to it.
