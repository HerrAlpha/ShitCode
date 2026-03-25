#!/bin/bash

# ============================================================
# PATHS — match your actual server layout
# Source  : /Users/herralpha/Development/project/ShitCode
# App out : /var/www/shitcode
# ============================================================
SRC_DIR="/Users/herralpha/Development/project/ShitCode"
APP_DIR="/var/www/shitcode"
DOMAIN="shitcode.intaraai.com"
EMAIL="admin@intaraai.com"
DB_NAME="error_log_dashboard"
DB_USER="root"
DB_PASS="MacBookAir2020@"
MYSQL_ROOT_PASS="MacBookAir2020@" # root password on this server
SERVICE="shitcode"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

# Ensure dotnet is in PATH
export DOTNET_ROOT="${DOTNET_ROOT:-/usr/local/share/dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH:/usr/share/dotnet:/usr/local/bin:$HOME/.dotnet"
DOTNET_BIN=$(which dotnet)

echo -e "${GREEN}=== Starting Deployment for $DOMAIN ===${NC}"

# 1. Firewall
echo -e "${GREEN}[1/8] Configuring Firewall...${NC}"
if command -v ufw > /dev/null; then
    sudo ufw allow OpenSSH
    sudo ufw allow 'Nginx Full'
else
    echo "UFW not found, skipping."
fi

# 2. MySQL
echo -e "${GREEN}[2/8] Configuring MySQL...${NC}"
sudo mysql -u root -p"$MYSQL_ROOT_PASS" -e "CREATE DATABASE IF NOT EXISTS $DB_NAME;"
sudo mysql -u root -p"$MYSQL_ROOT_PASS" -e "CREATE USER IF NOT EXISTS '$DB_USER'@'localhost' IDENTIFIED BY '$DB_PASS';"
sudo mysql -u root -p"$MYSQL_ROOT_PASS" -e "GRANT ALL PRIVILEGES ON $DB_NAME.* TO '$DB_USER'@'localhost';"
sudo mysql -u root -p"$MYSQL_ROOT_PASS" -e "FLUSH PRIVILEGES;"

# 3. STOP SERVICE BEFORE PUBLISH
echo -e "${GREEN}[3/8] Stopping service (releases file locks)...${NC}"
sudo systemctl stop $SERVICE 2>/dev/null || true
sleep 2

# 4. Build Frontend (Tailwind/CSS)
echo -e "${GREEN}[4/8] Building Frontend Assets...${NC}"
cd "$SRC_DIR/ErrorLogDashboard.Web"
npm install
npm run css:build

# 5. Publish App
echo -e "${GREEN}[5/8] Publishing App to $APP_DIR...${NC}"
mkdir -p "$APP_DIR"
cd "$SRC_DIR"
dotnet publish "ErrorLogDashboard.Web/ErrorLogDashboard.Web.csproj" -c Release -o "$APP_DIR" --runtime linux-x64 --self-contained false
if [ $? -ne 0 ]; then
    echo -e "${RED}ERROR: Publish failed! Restarting old service and aborting...${NC}"
    sudo systemctl start $SERVICE
    exit 1
fi

# Create wwwroot and logs directories
echo -e "${GREEN}  Ensuring directories and permissions...${NC}"
mkdir -p "$APP_DIR/wwwroot"
# Add any specific upload folders if needed:
# mkdir -p "$APP_DIR/wwwroot/uploads" 
chown -R www-data:www-data "$APP_DIR" 2>/dev/null || true
chmod -R 775 "$APP_DIR" 2>/dev/null || true

# 6. Production appsettings
echo -e "${GREEN}[6/8] Writing appsettings.Production.json...${NC}"
cat <<EOF > "$APP_DIR/appsettings.Production.json"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=$DB_NAME;User=$DB_USER;Password=$DB_PASS;"
  },
  "DeepSeek": {
    "ApiKey": "sk-3e78fb32e1e84cbbb74c1b1a04c7e51c"
  }
}
EOF

# 7. Nginx Config
echo -e "${GREEN}[7/8] Writing Nginx config...${NC}"
cat <<EOF > /etc/nginx/sites-available/$SERVICE
server {
    server_name $DOMAIN;

    client_max_body_size 1024M;

    location / {
        proxy_pass http://localhost:5009;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_set_header X-Forwarded-Host \$host;
        proxy_buffering off;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
        proxy_read_timeout 300;
    }

    listen 80;
}
EOF

sudo ln -sf /etc/nginx/sites-available/$SERVICE /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx

# 8. Systemd Service
echo -e "${GREEN}[8/8] Writing systemd service and restarting...${NC}"
cat <<EOF > /etc/systemd/system/$SERVICE.service
[Unit]
Description=ShitCode Error Dashboard .NET App
After=network.target mysql.service

[Service]
WorkingDirectory=$APP_DIR
ExecStart=$DOTNET_BIN $APP_DIR/ErrorLogDashboard.Web.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=$SERVICE
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_ROOT=$DOTNET_ROOT
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_URLS=http://localhost:5009

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable $SERVICE.service
sudo systemctl restart $SERVICE.service

echo -e "${GREEN}Waiting for service to start...${NC}"
sleep 3
sudo systemctl status $SERVICE --no-pager -l

# SSL
echo -e "${GREEN}Running Certbot for SSL...${NC}"
sudo certbot --nginx -d $DOMAIN --non-interactive --agree-tos -m $EMAIL --redirect

echo -e "${GREEN}=== Deployment Complete! Visit https://$DOMAIN ===${NC}"
