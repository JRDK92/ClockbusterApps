# ⏱️ Clockbuster Apps

> **Know where your time goes.** A lightweight Windows app that automatically tracks your application usage.

## What is Clockbuster?

Clockbuster runs quietly in the background and logs every application you use. See what you worked on, when you used it, and for how long. Perfect for tracking billable hours, analyzing productivity, or understanding your digital habits.

**Your data stays local.** No cloud, no tracking, no telemetry.

## Features

- **Automatic Tracking** - Launch and forget. Every app is logged with timestamps and duration
- **Session History** - View your complete usage history in a clean, sortable table
- **Smart Selection** - Click, multi-select (Ctrl+Click), or select all (Ctrl+A) to manage sessions
- **Ignore List** - Right-click any app to stop tracking it permanently
- **Privacy First** - Everything stays on your machine in a local SQLite database

## Quick Start

1. Download the latest release
2. Run `ClockbusterApps.exe`
3. Click **Start** to begin tracking

That's it. Click **View Data** anytime to see your session history.

## Under the Hood

Clockbuster uses native Windows APIs to detect which application has focus. Sessions are automatically started when you switch to a new app and closed when you switch away or close it. All data is stored locally in SQLite for fast, reliable access. The app runs with minimal resource usage and respects your privacy - no data ever leaves your computer.

---

**Built with C# and WPF** • **Requires .NET 8.0 Runtime** • **Windows 10/11**