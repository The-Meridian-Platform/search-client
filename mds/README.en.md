# 🖥️ Meridian OSINT Client — Documentation

> Comprehensive technical documentation for Meridian OSINT Client: modern WPF desktop application for real-time cyber reconnaissance streaming.

---

## 📑 Table of Contents

1. [Overview & Architecture](#-overview--architecture)
2. [Technology Stack](#-technology-stack)
3. [Key Modules & Capabilities](#-key-modules--capabilities)
4. [Project Structure](#-project-structure)
5. [Prerequisites & Build](#-prerequisites--build)
6. [Connection & Backend Pairing](#-connection--backend-pairing)
7. [Unit Testing](#-unit-testing)

---

## 🏛️ Overview & Architecture

Meridian Client connects to the `MeridianHost.Api` OSINT search engine and provides an analyst-focused desktop workspace for conducting reconnaissance, examining live results, reviewing session logs, and browsing past searches.

The client is built on strict **Model-View-ViewModel (MVVM)** principles powered by `CommunityToolkit.Mvvm`:
- **Loose Coupling:** Clean boundary between presentation (XAML views) and business logic (ViewModels).
- **Thread Safety:** Thread-safe collection synchronization using `BindingOperations.EnableCollectionSynchronization` for high-throughput gRPC stream message handling.
- **Micro-Interactions & Transitions:** Fluid kinematic animations (`CubicEase`, `TranslateTransform`, `DoubleAnimation`, `Storyboard`) providing a Linear/Raycast dark theme feel.

```
                    ┌────────────────────────┐
                    │    Meridian Host       │
                    │   (.NET 10 Web API)    │
                    └───────────┬────────────┘
                                │
               ┌────────────────┴────────────────┐
     REST API (POST / GET)              gRPC HTTP/2 Streaming
  (Session lifecycle & History)       (FindingDiscovered events)
               │                                 │
               ▼                                 ▼
       [MeridianApiClient]             [MeridianStreamClient]
               │                                 │
               └────────────────┬────────────────┘
                                │
                                ▼
                       [SearchViewModel]
                                │ (Thread-safe collection sync)
                                ▼
                       [FindingItemViewModel]
                                │
                                ▼
                         [SearchView]
```

---

## 🛠️ Technology Stack

| Component | Technology | Description |
|---|---|---|
| **Platform** | .NET 10 (`net10.0-windows`) | High-performance C# runtime with AOT and modern JIT optimizations |
| **UI Framework** | WPF + `WPF-UI` (Lepoco) | Windows 11 Fluent dark mode styling, Mica/Acrylic backdrops |
| **MVVM Library** | `CommunityToolkit.Mvvm` | Source-generated `[ObservableProperty]`, `[RelayCommand]` |
| **Streaming & RPC** | `Grpc.Net.Client` & `Google.Protobuf` | HTTP/2 Protobuf server-streaming client |
| **HTTP & REST** | `System.Net.Http.Json` | Asynchronous REST operations with custom socket handler |
| **Testing** | `xUnit` | Automated test suite verifying ViewModels, parsers, and value converters |

---

## 🚀 Key Modules & Capabilities

### 1. Live Target Reconnaissance (`SearchView`)
- **Target Formulation:** Supports `Auto`, `Domain`, `IpAddress`, `Email`, and `Username`.
- **HUD Telemetry Strip:** Displays live elapsed execution time (`mm:ss.fff`), session status (`Ready`, `Pending`, `Running`, `Completed`, `Failed`), and category counters.
- **Categorized Finding Cards:**
  - **DNS:** Visual pills for `A`, `AAAA`, `MX`, `TXT`, `NS` records with TTL and priority values.
  - **WHOIS / RDAP:** Registrar identity, domain status, creation/expiration dates, and geographic information.
  - **Subdomains:** Discovered subdomains, certificate authority issuer chip, and validity range.
  - **Web Technologies:** HTTP response code badges (`200 OK`, `301`, etc.), page `<title>`, web server & framework badges, and security posture checks (`HSTS`, `CSP`, `X-Frame`).
  - **Raw JSON Inspection:** Accordion expandable JSON viewer with one-click clipboard copy.
- **Fast Interactive Clear:** Animated character-by-character backspace erasing for textboxes and brisk sequential card dismissal.

### 2. Collapsible Filter Bar
- Toggleable category filtering via neon `+` button with 45° rotation.
- Instant client-side search across all fields (IPs, hosts, types, issuers, headers, servers).

### 3. Session History (`HistoryView`)
- Historical audit log of conducted OSINT searches with creation time and total finding counts.
- Instant reload of full session findings directly into the search view.
- Built-in session deletion endpoint integration.

### 4. Telemetry & Log Console (`LogsView`)
- Real-time diagnostic viewer capturing REST queries, gRPC streaming events, and background health checks.
- Filter by level (`All`, `Info`, `Warning`, `Error`, `Debug`) and quick text search.
- One-click copy of all filtered logs to the clipboard.

### 5. Settings & Health Checks (`SettingsView`)
- Configurable base backend URL (supports HTTPS `https://localhost:7013` and HTTP `http://localhost:5168`).
- Unencrypted HTTP/2 support (`SocketsHttpHandler.Http2UnencryptedSupport`).
- Ping / latency probe with visual status pill.

---

## 📂 Project Structure

```
MeridianClient/
├── App.xaml / App.xaml.cs                   # Application entry, DI setup, theme initialization
├── MainWindow.xaml / MainWindow.xaml.cs     # Main shell, sidebar navigation, titlebar
├── MeridianClient.csproj                    # Project configuration & Protobuf compilation
├── MeridianClient.slnx                      # Solution definition
├── Converters/
│   └── ValueConverters.cs                  # Value converters (Visibility, Brush, String matching)
├── Models/
│   ├── AppSettings.cs                       # Client settings model
│   ├── CreateSearchSessionRequest.cs        # REST session creation request
│   ├── CreateSearchSessionResponse.cs       # REST session creation response
│   ├── FindingModel.cs                      # Generic finding payload model
│   ├── LogEntry.cs                          # Telemetry log entry model
│   ├── ParsedDnsData.cs                     # DNS record representation
│   ├── ParsedHttpTechData.cs                # Web technology & security headers model
│   ├── ParsedSubdomainData.cs               # Subdomain & certificate model
│   ├── ParsedWhoisData.cs                   # WHOIS / RDAP model
│   ├── SearchSessionModel.cs                # Session metadata and findings container
│   └── SearchTarget.cs                      # Reconnaissance target descriptor
├── Protos/
│   └── search_stream.proto                  # Protobuf contract for SearchStreamService
├── Services/
│   ├── HealthCheckService.cs                # Periodic and on-demand API health ping
│   ├── LogService.cs                        # In-memory logging and event bus
│   ├── MeridianApiClient.cs                 # REST API client
│   ├── MeridianStreamClient.cs              # gRPC client stream wrapper
│   └── SettingsService.cs                   # Persistent configuration storage
├── ViewModels/
│   ├── FindingItemViewModel.cs              # Finding card ViewModel and payload parser
│   ├── HistoryViewModel.cs                  # Session history ViewModel
│   ├── LogsViewModel.cs                     # Diagnostic logs console ViewModel
│   ├── MainViewModel.cs                     # Shell & navigation orchestrator
│   ├── SearchViewModel.cs                   # Search coordinator & stream handler
│   └── SettingsViewModel.cs                 # Gateway settings ViewModel
├── Views/
│   ├── HistoryView.xaml                     # Session history UI
│   ├── LogsView.xaml                        # Log viewer UI
│   ├── SearchView.xaml                      # Main reconnaissance feed UI
│   └── SettingsView.xaml                    # Settings UI
└── Tests/
    └── MeridianClient.Tests/                # Automated xUnit test suite (34 unit tests)
```

---

## ⚙️ Prerequisites & Build

- **Operating System:** Windows 10 (Build 19041+) or Windows 11
- **SDK:** .NET 10.0 SDK
- **IDE:** JetBrains Rider or Visual Studio 2022+

### Build & Run:

```bash
# Restore dependencies and build
dotnet build MeridianClient.slnx

# Run desktop client
dotnet run --project MeridianClient.csproj
```

---

## 🧪 Unit Testing

Execute all unit tests using the .NET CLI:

```bash
dotnet test Tests/MeridianClient.Tests/MeridianClient.Tests.csproj
```
