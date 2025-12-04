# Installation Guide

## Prerequisites
- .NET 8.0 SDK
- MySQL Server
- Node.js & NPM

## Build Steps

1. **Clone the repository**
   ```bash
   git clone <repo-url>
   cd ErrorLogDashboard
   ```

2. **Database Setup**
   - Create a MySQL database named `error_log_dashboard`.
   - Update `appsettings.json` with your connection string.

3. **Build Frontend**
   ```bash
   cd ErrorLogDashboard.Web
   npm install
   npm run css:build
   ```

4. **Build Backend**
   ```bash
   cd ..
   dotnet publish ErrorLogDashboard.Web/ErrorLogDashboard.Web.csproj -c Release -o ./publish
   ```

## Deployment (Linux/Systemd)

1. **Copy Files**
   Copy the contents of `./publish` to `/var/www/error-log-dashboard`.

2. **Setup Service**
   - Copy `error-log-dashboard.service` to `/etc/systemd/system/`.
   - Reload daemon: `sudo systemctl daemon-reload`.
   - Enable and start: `sudo systemctl enable --now error-log-dashboard`.

## CLI Usage (Laravel-like)

Use the `artisan` tool (built CLI) to scaffold files.
First build the CLI:
```bash
dotnet build ErrorLogDashboard.Cli/ErrorLogDashboard.Cli.csproj
```

Run commands:
```bash
./ErrorLogDashboard.Cli/bin/Debug/net8.0/ErrorLogDashboard.Cli make:model Customer
./ErrorLogDashboard.Cli/bin/Debug/net8.0/ErrorLogDashboard.Cli make:controller Customer
```
