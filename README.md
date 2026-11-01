# 🖥️ Meridian OSINT Client

> High-performance Cyber OSINT Desktop Client & Real-time gRPC Streaming Interface built with .NET 10, WPF, and Modern Fluent UI.

Meridian Client is the official companion desktop application for **Meridian Search Platform** (`MeridianHost`). It connects to the high-throughput search engine via REST API and streams live OSINT intelligence cards in real-time over HTTP/2 **gRPC Server Streaming**.

---

## ⚡ Key Highlights

- 🛸 **Cyber OSINT Aesthetic:** Dark Linear / Raycast inspired UI with neon accents, glowing status indicators, and smooth animations.
- 📡 **Real-time gRPC Streaming:** Instant live feed of incoming findings (DNS, WHOIS, Subdomains, Web Tech stack) as they are discovered by backend workers.
- 🎯 **Multi-Category Findings Support:**
  - **DNS Records:** Color-coded badges (`A`, `AAAA`, `MX`, `TXT`, `NS`), resolved values, TTLs, and MX priorities.
  - **WHOIS & RDAP:** Clean visual cards for Registrar, creation/expiration dates, status, and country code.
  - **Subdomains:** Discovered subdomains via Certificate Transparency logs (`crt.sh`) with issuer and validity details.
  - **Web Technologies:** HTTP status code badges, page `<title>`, web server and framework chips, and security headers posture (`HSTS`, `CSP`, `X-Frame`).
- 🎛️ **Quick Categorization & Search:** Smooth collapsible filter bar with quick toggle (`+`), category tabs, and sub-millisecond search across all fields.
- 📜 **Interactive Session History:** View, search, inspect, and delete previously conducted reconnaissance sessions.
- 📊 **Dedicated Logs Console:** Live telemetry viewer with search, level filters (`Info`, `Warning`, `Error`, `Debug`), and one-click clipboard export.
- 🧹 **Satisfying Micro-Interactions:** Smooth flying-away cards on clear, typewriter/backspace erasing effect, and animated navigation tabs.

---

## 🌐 Documentation / Документация

- 🇬🇧 **[English Documentation](mds/README.en.md)** (Architecture, MVVM design, setup guide)
- 🇷🇺 **[Документация на русском](mds/README.ru.md)** (Архитектура, стек, запуск и интеграция)

---

## 📄 License

Dual-licensed under **GNU AGPLv3** (Free & Open Source) and **Commercial License**. Part of The Meridian Platform.
