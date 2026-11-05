# 🖥️ Meridian OSINT Client

> High-performance Cyber OSINT Desktop Client, Interactive Force-Directed Knowledge Graph & Real-time gRPC Streaming Interface built with .NET 10, WPF, and Modern Fluent UI.

Meridian Client is the official companion desktop application for **Meridian Search Platform** (`MeridianHost`). It connects to the high-throughput reconnaissance search engine via REST API and streams live OSINT intelligence cards in real-time over HTTP/2 **gRPC Server Streaming**, automatically projecting relationships onto an interactive GPU-accelerated Knowledge Graph.

---

## ⚡ Key Highlights

- 🛸 **Cyber OSINT Aesthetic:** Dark & Light Linear / Raycast inspired UI with neon accents, glowing status indicators, smooth animations, and dynamic theme switching.
- 🌐 **Interactive Force-Directed Knowledge Graph:**
  - Real-time physics simulation (Coulomb repulsion, Hooke spring attraction, center gravity).
  - Highload optimization with **Auto-Sleep / Damping** (0.0% CPU when stabilized) and localized physics on drag.
  - **WPF Freezing & Line Pooling:** Zero allocation on render frames, DirectWrite GPU batching.
  - **Level of Detail (LOD):** Seamless zooming with automatic decluttering (labels collapse when zoomed out).
  - **Minimap Overview & Safety Bounds:** Real-time minimap with viewport navigation and 200-node canvas limit protection.
  - **Smart Layout & Clustering:** Hierarchical tree organize, threat path tracing, and node expand (Pivot) operations.
- 📡 **Real-time gRPC Streaming:** Instant live feed of incoming findings (DNS, WHOIS, Subdomains, Web Tech stack) as they are discovered by backend workers.
- 🎯 **Multi-Category Findings Support:**
  - **DNS Records:** Color-coded badges (`A`, `AAAA`, `MX`, `TXT`, `NS`), resolved values, TTLs, and MX priorities.
  - **WHOIS & RDAP:** Clean visual cards for Registrar, creation/expiration dates, status, and country code.
  - **Subdomains:** Discovered subdomains via Certificate Transparency logs (`crt.sh`) with issuer and validity details.
  - **Web Technologies:** HTTP status code badges, page `<title>`, web server and framework chips, and security headers posture (`HSTS`, `CSP`, `X-Frame`).
- 🗂️ **Investigation Projects & Standalone Hub:**
  - Dedicated welcome screen (Hub) for managing multi-target investigations.
  - Persistent offline storage (`investigations.json`) and session isolation.
  - Instant export of graph layouts to high-DPI vector/PNG images.
- 🎛️ **Quick Categorization & Search:** Smooth collapsible filter bar with quick toggle (`+`), category tabs, and sub-millisecond search across all fields.
- 📜 **Interactive Session History:** View, search, inspect, and delete previously conducted reconnaissance sessions with investigation scoping.
- 📊 **Dedicated Logs Console:** Live telemetry viewer with search, level filters (`Info`, `Warning`, `Error`, `Debug`), and one-click clipboard export.

---

## 🌐 Documentation / Документация

- 🇬🇧 **[English Documentation](mds/README.en.md)** (Architecture, MVVM design, setup guide, graph physics)
- 🇷🇺 **[Документация на русском](mds/README.ru.md)** (Архитектура, граф связей, оптимизации, запуск)

---

## 📄 License

Dual-licensed under **GNU AGPLv3** (Free & Open Source) and **Commercial License**. Part of The Meridian Platform.
