# LocalShare

> **LAN-Only P2P File Sharing, Real-Time Chat & Public Space Sharing App for Windows**

LocalShare is a zero-configuration, decentralized peer-to-peer Windows application built on **.NET 10**. It operates without any internet connection or central server—every machine on the local network acts as a full peer running UDP beacon discovery, an embedded ASP.NET Core Kestrel server, SignalR chat hub, and SQLite storage.

---

## 🚀 Features

- **P2P Peer Discovery**: Zero-config LAN discovery using UDP multicast announcements on `239.255.10.10:53210`.
- **Direct P2P File Transfers**: High-speed chunked HTTP file transfers with SHA256 integrity verification, saved under `%LOCALAPPDATA%\LocalShare\Received\<SenderDisplayName>\`.
- **Real-Time 1:1 Chat**: Embedded self-hosted SignalR WebSockets (`/hub/chat`) with typing indicators and drag-and-drop chat attachments.
- **Public Space Folder Sharing**: Expose a local directory read-only over HTTP with Range header support for resumable file browsing and downloading by LAN peers.
- **Group Management & Fan-Out Chat**: Local group rosters with P2P fan-out messaging to all online members.
- **Windows 11 Fluent UI**: WPF application built with `WPF-UI`, `CommunityToolkit.Mvvm`, navigation rail, and responsive layout behavior.

---

## 🛠️ Architecture & Solution Layout

Built on **.NET 10** (`net10.0-windows` for WPF UI, `net10.0` for class libraries):

```
LocalShare.slnx / LocalShare.sln
├── src/
│   ├── LocalShare.Common/        - Result pattern, Constants, Network helpers
│   ├── LocalShare.Core/          - Domain Models (Peer, Profile, Message, Group, TransferItem, PublicShareEntry) & Interfaces
│   ├── LocalShare.Data/          - SQLite DatabaseInitializer & SqliteRepositories (Dapper + Microsoft.Data.Sqlite)
│   ├── LocalShare.Networking/    - UDP Discovery Service, PeerRegistry, Kestrel HTTP Host, SignalR ChatHub & Transfer Engine
│   └── LocalShare.App/           - WPF Desktop App with WPF-UI Fluent design, ViewModels, Views, and Adaptive Layout
└── tests/
    ├── LocalShare.Core.Tests/     - Unit tests for Core domain models
    ├── LocalShare.Networking.Tests/- Unit tests for Peer discovery registry and timeouts
    └── LocalShare.Data.Tests/     - Integration tests for SQLite schema and repositories
```

---

## 📋 Prerequisites

- **SDK**: [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or higher installed.
- **OS**: Windows 10 / Windows 11 (x64).

---

## 🔧 Building the Project

Clone or navigate to the repository folder:

```powershell
cd d:\dotnet\LocalShare
```

Build the entire solution:

```powershell
dotnet build LocalShare.slnx
```

---

## 🧪 Running Unit Tests

Run all unit and integration tests across the solution:

```powershell
dotnet test LocalShare.slnx
```

---

## 🏃 Running the Application

Launch the WPF UI desktop application:

```powershell
dotnet run --project src/LocalShare.App/LocalShare.App.csproj
```

Alternatively, publish as a self-contained single-file Windows executable:

```powershell
dotnet publish src/LocalShare.App/LocalShare.App.csproj -c Release -r win-x64 --self-contained
```

---

## 📁 Storage & Configuration

- **Received Files**: `%LOCALAPPDATA%\LocalShare\Received\<SenderDisplayName>\`
- **Database File**: `%LOCALAPPDATA%\LocalShare\localshare.db`
- **Default UDP Multicast**: `239.255.10.10:53210`
- **Default HTTP Server**: `http://0.0.0.0:53211`

---

## 📜 License

MIT License.

powershell -File .\build-release.ps1
powershell -File .\build-release.ps1 -Version 2.0.4