# ErrorLog Dashboard

A comprehensive error tracking dashboard built with ASP.NET Core, featuring real-time error logging, AI-powered summaries, and multi-language support.

## Features

- 🔐 **Authentication & Authorization** - Cookie-based auth with role management
- 📊 **Dashboard** - View projects, error statistics, and recent errors
- 🤖 **AI Summaries** - Automatic error summaries using DeepSeek AI
- 🔑 **Secure API** - API Key + Security Key authentication
- 👥 **User Management** - Admin panel for managing users and projects
- 💎 **Subscription Plans** - Free, Basic, Pro, and Platinum tiers
- 📚 **Documentation** - Complete integration guides for 7+ languages
- 🌐 **Multi-Language** - SDKs for .NET, Node.js, Python, PHP, Go, Java, Ruby

## Prerequisites

- .NET 9.0 SDK
- MySQL Server
- DeepSeek API Key (for AI summaries)

## Setup Instructions

### 1. Database Configuration

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=error_log_dashboard;user=root;password=yourpassword"
  }
}
```

### 2. DeepSeek API Key

For local development, create `appsettings.Development.json` (this file is gitignored):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "DeepSeek": {
    "ApiKey": "sk-YOUR_DEEPSEEK_API_KEY_HERE"
  }
}
```

**For production**, use environment variables:
```bash
export DeepSeek__ApiKey="sk-YOUR_DEEPSEEK_API_KEY_HERE"
```

Or use .NET User Secrets:
```bash
cd ErrorLogDashboard.Web
dotnet user-secrets set "DeepSeek:ApiKey" "sk-YOUR_DEEPSEEK_API_KEY_HERE"
```

### 3. Run the Application

```bash
cd ErrorLogDashboard.Web
dotnet run
```

The application will be available at `https://localhost:5001`

## Default Accounts

The database is seeded with the following test accounts:

- **Admin**: `admin@example.com` / `hashed_password`
- **Free User**: `free@example.com` / `pwd`
- **Basic User**: `basic@example.com` / `pwd`
- **Pro User**: `pro@example.com` / `pwd`
- **Platinum User**: `platinum@example.com` / `pwd`

## Project Structure

```
ErrorLogDashboard.Web/
├── Controllers/        # MVC Controllers
├── Models/            # Data models
├── Views/             # Razor views
├── Services/          # Business logic (DeepSeek, etc.)
├── Data/              # Database context and seeder
└── wwwroot/           # Static files

ErrorLogDashboard.Cli/ # CLI tool for testing
```

## API Usage

Send errors to your webhook URL:

```bash
curl -X POST https://your-domain.com/api/ingest/{API_KEY} \
  -H "Content-Type: application/json" \
  -H "X-Security-Key: {SECURITY_KEY}" \
  -d '{
    "message": "Error occurred",
    "stackTrace": "at Program.Main() in Program.cs:line 42"
  }'
```

## Documentation

Visit `/Docs` in the application for complete integration guides for:
- .NET / C#
- Node.js / JavaScript
- Python
- PHP
- Go
- Java
- Ruby

## Development Notes

- Database schema is recreated on each startup (development mode)
- For production, use proper EF Core migrations
- API keys are stored in plain text for demo purposes

## Security Recommendations

1. **Never commit API keys** - Use environment variables or User Secrets
2. **Enable HTTPS** - Use valid SSL certificate in production
3. **Password hashing** - Current implementation uses placeholder hashing
4. **Database** - Use proper migrations instead of EnsureDeleted/Created
5. **API Rate Limiting** - Implement rate limiting for production

## License

MIT License
