# 🖥️ Meridian OSINT Client — Documentation

> Comprehensive technical documentation for Meridian OSINT Client: modern C# WPF desktop application for real-time cyber intelligence streaming, interactive force-directed graph visualization, and multi-session investigation workflows.

---

## 📑 Table of Contents

1. [Overview & Architecture](#-overview--architecture)
2. [Technology Stack](#-technology-stack)
3. [Key Modules & Features](#-key-modules--features)
4. [Interactive Knowledge Graph & Optimizations](#-interactive-knowledge-graph--optimizations)
5. [Project Structure](#-project-structure)
6. [Prerequisites & Getting Started](#-prerequisites--getting-started)
7. [Backend Integration](#-backend-integration)

---

## 🏛️ Overview & Architecture

**Meridian Client** is the official companion desktop application for **Meridian Search Platform** (`MeridianHost.Api`). It provides intelligence analysts with a sleek, Cyber OSINT (Linear / Raycast inspired) workstation to run reconnaissance, inspect live findings, explore threat networks through an interactive knowledge graph, manage investigations, and monitor system telemetry.

The application follows strict **Model-View-ViewModel (MVVM)** principles powered by `CommunityToolkit.Mvvm`:
- **Decoupled Architecture:** Clean boundary between declarative XAML views and testable view models.
- **Thread Safety:** Reactive thread-safe collection synchronization via `BindingOperations.EnableCollectionSynchronization` during high-frequency gRPC server streaming.
- **Fluid Micro-Interactions:** Kinematic tab transitions, sliding panels, typewriter backspace input erasing, and glowing badges.
- **Dual Theme Support:** Seamless Dark and Light theme switching with dynamic contrast adjustments for graph nodes, edges, and the minimap.

```
                    ┌────────────────────────┐
                    │    Meridian Host       │
                    │   (.NET 10 Web API)    │
                    └───────────┬────────────┘
                                │
               ┌────────────────┴────────────────┐
     REST API (POST / GET)              gRPC HTTP/2 Streaming
  (Session lifecycle, history)         (FindingDiscovered events)
               │                                 │
               ▼                                 ▼
       [MeridianApiClient]             [MeridianStreamClient]
               │                                 │
               └────────────────┬────────────────┘
                                │
               ┌────────────────┴────────────────┐
               ▼                                 ▼
      [SearchViewModel]                  [GraphViewModel]
               │                                 │
     (Findings UI stream)               (Force-Directed Physics Engine)
               │                                 │
               ▼                                 ▼
         [SearchView]                       [GraphView]
```

---

## 🛠️ Technology Stack

| Component | Technology | Description |
|---|---|---|
| **Platform** | .NET 10 (`net10.0-windows`) | High-performance C# runtime with tiered JIT compilation |
| **UI Framework** | WPF + `WPF-UI` (Lepoco) | Windows 11 Fluent Dark / Light styling, Mica acrylic effects |
| **MVVM** | `CommunityToolkit.Mvvm` | Source-generated `[ObservableProperty]`, `[RelayCommand]` |
| **gRPC & Streaming** | `Grpc.Net.Client` & `Google.Protobuf` | HTTP/2 server streaming client |
| **REST & Networking**| `System.Net.Http.Json` | Asynchronous HTTP client with custom sockets handler |
| **Graph Physics** | Custom Force-Directed Engine | Coulomb repulsion, Hooke springs, gravitational centering |
| **Investigation Storage**| JSON / Local Storage | Offline persistent investigations and graph states |

---

## 🚀 Key Modules & Features

### 1. Welcome Hub (`WelcomeWindow`)
- Dedicated entry screen for managing multi-target investigation projects.
- **Quick Actions:** Start fresh live searches, open saved investigations (`investigations.json`), or import external sessions.
- Filterable list of ongoing and archived investigations with target badges and findings count.
- Seamless single-click transition between the Hub and the active Workspace.

### 2. Live Target Reconnaissance (`SearchView`)
- **Target Inputs:** Supports `Auto`, `Domain`, `IpAddress`, `Email`, and `Username`.
- **Telemetry HUD Bar:** Live session timer (`mm:ss.fff`), status badge (`Ready`, `Pending`, `Running`, `Completed`, `Failed`), and category counters.
- **Rich Intelligence Cards:**
  - **DNS Records:** Color-coded badges (`A`, `AAAA`, `MX`, `TXT`, `NS`), resolved values, TTLs, and MX priorities.
  - **WHOIS & RDAP:** Registrar, domain status, creation/expiration dates, and country codes.
  - **Subdomains:** Certificate Transparency log findings (`crt.sh`) with issuer and certificate validity dates.
  - **Web Technologies:** HTTP status codes (`200 OK`, `301`), page `<title>`, web servers, runtime chips, and security headers (`HSTS`, `CSP`, `X-Frame`).
  - **Raw JSON Inspector:** Collapsible code view with one-click clipboard copying.

### 3. Session History (`HistoryView`)
- Historical database of completed OSINT sessions stored in PostgreSQL.
- Investigation-scoped filtering: inspect only sessions belonging to the active project or explore all global sessions.
- Instant rehydration of session findings back onto the canvas or graph.
- Granular deletion of obsolete sessions.

### 4. Telemetry & Logs Viewer (`LogsView`)
- Real-time logging console capturing REST API, gRPC events, and Health Checks.
- Granular log level filters (`All`, `Info`, `Warning`, `Error`, `Debug`) with search query matching.
- One-click clipboard export for debugging and reporting.

### 5. Settings & Network Diagnostics (`SettingsView`)
- Base API and gRPC endpoint toggles (HTTPS `https://localhost:7013` and HTTP `http://localhost:5168`).
- Unencrypted HTTP/2 support (`SocketsHttpHandler.Http2UnencryptedSupport`).
- Real-time gateway ping monitoring with latency measurement.

---

## 🕸️ Interactive Knowledge Graph & Optimizations (`GraphView`)

Engineered for large-scale OSINT visualization (150+ nodes and 190+ edges) at buttery-smooth **60–120 FPS**.

### Force-Directed Physics Engine:
- **Coulomb Repulsion:** Pairwise node repulsion $O(N^2)$ with distant pair pruning ($>650$ px) to prevent unnecessary calculations.
- **Hooke's Spring Law:** Distance-based spring attraction pulling related nodes into readable clusters.
- **Centering Gravity:** Gentle drift toward canvas center to maintain cohesive layouts.

### 4 Pillars of Highload UI Optimization:
1. **Physics Auto-Sleep & Damping:**
   - Tracks kinetic energy and layout step counts (`MaxIterations = 140`).
   - Automatically stops the physics loop (`_physicsTimer.Stop()`) when the graph stabilizes, reducing CPU utilization to **0.0%**.
   - Wakes up (`WakeUp()`) only on user interaction (node drag, expand/pivot, or new finding arrival).
   - Node dragging computes **localized physics** solely for immediate neighbors.
2. **GPU Batching & WPF Freezing:**
   - All brushes (`SolidColorBrush`), pens (`Pen`), and shapes are frozen via `.Freeze()`, eliminating Dispatcher change tracking.
   - Reusable edge line pooling (`_edgeLinesPool`): eliminates allocations of `Line` objects per frame, updating only coordinates `X1, Y1, X2, Y2`.
3. **Level of Detail (LOD) on Zoom:**
   - Zooming out (`ZoomScale < 0.55`) hides node labels and secondary text, eliminating DirectWrite glyph layout overhead.
   - Zooming in (`ZoomScale >= 0.55`) seamlessly restores full typography and badges.
4. **Canvas Safety Bounds:**
   - Built-in threshold of `MaxCanvasNodes = 200`.
   - Prevents UI freezes during massive subdomain expansions with informative warning chips and notifications.

### Graph Investigation Tools:
- **Node Expansion (Pivot):** Double-click or context-menu expand any node (subdomain, IP, email) to trigger secondary discovery streams.
- **Branch Collapsing:** Collapse dense clusters into numeric badges indicating hidden node counts.
- **Threat Path Tracing:** Animated pulsating neon path highlighting chains from compromised nodes to roots.
- **Interactive Minimap:** Real-time mini radar showing the entire graph boundary and current viewport frame with click/pan navigation.
- **High-DPI Export:** One-click vector/high-resolution PNG graph snapshot export for analytical reports.

---

## 📂 Project Structure

```
MeridianClient/
├── App.xaml / App.xaml.cs                   # Entry point, DI container, theme initialization
├── MainWindow.xaml / MainWindow.xaml.cs     # Main application window, sidebar, screen router
├── WelcomeWindow.xaml / .xaml.cs            # Fullscreen Welcome & Investigation Hub
├── MeridianClient.csproj                    # Project configuration & Protobuf compilation
├── Converters/
│   └── ValueConverters.cs                  # UI value converters (Visibility, Brushes, etc.)
├── Models/
│   ├── AppSettings.cs                       # Client settings
│   ├── GraphModels.cs                       # GraphNode, GraphEdge, and NodeType definitions
│   ├── InvestigationModel.cs                # Investigation container model
│   ├── FindingModel.cs                      # Generic intelligence finding model
│   └── SearchSessionModel.cs                # Reconnaissance session model
├── Services/
│   ├── GraphDataMapper.cs                   # Maps OSINT findings to graph topology
│   ├── GraphSimulationEngine.cs             # Physics engine with auto-sleep & local stepping
│   ├── InvestigationService.cs              # JSON-based investigation storage
│   ├── MeridianApiClient.cs                 # REST API communication
│   ├── MeridianStreamClient.cs              # gRPC streaming client
│   └── ThemeService.cs                      # Dynamic Dark/Light theme manager
├── ViewModels/
│   ├── GraphViewModel.cs                    # Graph state, layout, zooming, and pivot logic
│   ├── HistoryViewModel.cs                  # Session history & investigation filtering
│   ├── SearchViewModel.cs                   # Reconnaissance search & findings pipeline
│   ├── WelcomeViewModel.cs                  # Welcome Hub actions & recent investigations
│   └── MainViewModel.cs                     # Root navigation coordinator
└── Views/
    ├── GraphView.xaml / .xaml.cs            # Force-directed canvas and minimap
    ├── HistoryView.xaml / .xaml.cs          # Historical session browser
    ├── LogsView.xaml / .xaml.cs             # Real-time event log console
    ├── SearchView.xaml / .xaml.cs           # Target search input and finding cards
    └── SettingsView.xaml / .xaml.cs         # Settings & network monitor
```

---

## ⚙️ Prerequisites & Getting Started

- **OS:** Windows 10 (Build 19041+) or Windows 11
- **SDK:** .NET 10.0 SDK
- **IDE:** JetBrains Rider or Visual Studio 2022+

### Build & Run:

```bash
# Build project
dotnet build MeridianClient.csproj

# Run application
dotnet run --project MeridianClient.csproj
```
