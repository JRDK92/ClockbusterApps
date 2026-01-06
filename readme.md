# ⏱️ Clockbuster Apps

> **Know where your time goes.** A lightweight Windows app that automatically tracks your application usage.

[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-GPL%20v3-blue)](LICENSE)

## What is Clockbuster?

Clockbuster runs quietly in the background and logs every application you use. See what you worked on, when you used it, and for how long. Perfect for tracking billable hours, analyzing productivity, or understanding your digital habits.

**Your data stays local.** No cloud, no tracking, no telemetry.

## Features

- **Automatic Tracking** - Launch and forget. Every app is logged with timestamps and duration
- **Session History** - View your complete usage history in a clean, sortable table
- **Smart Selection** - Click, multi-select (Ctrl+Click), or select all (Ctrl+A) to manage sessions
- **Ignore List** - Right-click any app to stop tracking it permanently
- **Privacy First** - Everything stays on your machine in a local SQLite database

## Installation

**Requirements:** Windows 10/11 and [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

1. Download the latest release from the [Releases page](../../releases)
2. Extract and run `ClockbusterApps.exe`
3. Click **Start** to begin tracking

Click **View Data** anytime to see your session history.

## How It Works

Clockbuster uses native Windows APIs to detect which application has focus. Sessions are automatically started when you switch to a new app and closed when you switch away or close it. All data is stored locally in SQLite with minimal resource usage.

**Technical Stack:** C# 12 • .NET 8.0 • WPF • SQLite 3

## Usage & License

Clockbuster is free and open source software licensed under the [GNU General Public License v3.0](LICENSE). You are free to use, modify, and distribute this software under the terms of the GPL v3.

**Privacy Guarantee:** Your application usage data never leaves your computer. No cloud sync, no telemetry, no external connections.

## Contributing

Contributions are welcome! Open an issue or submit a pull request on [GitHub](../../issues).

---

**Built with ❤️ using C# and WPF**
