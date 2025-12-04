# Setup untuk Development

## DeepSeek API Key

Buat file `appsettings.Development.json` di folder `ErrorLogDashboard.Web/`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "DeepSeek": {
    "ApiKey": "sk-3e78fb32e1e84cbbb74c1b1a04c7e51c"
  }
}
```

File ini sudah ada di `.gitignore` sehingga tidak akan ter-commit ke Git.

## Database Connection

Sesuaikan connection string di `appsettings.json` dengan konfigurasi MySQL Anda.

## Run Application

```bash
cd ErrorLogDashboard.Web
dotnet run
```
