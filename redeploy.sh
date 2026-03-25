#!/bin/bash

# ShitCode Redeploy Script
# Quickly restarts the service and reloads Nginx.

SERVICE="shitcode"

# Colors
GREEN='\033[0;32m'
NC='\033[0m'

echo -e "${GREEN}🔄 Redeploying $SERVICE...${NC}"

# 1. Restart Service
sudo systemctl restart $SERVICE

# 2. Reload Nginx
echo -e "${GREEN}🌐 Reloading Nginx...${NC}"
sudo nginx -t && sudo systemctl reload nginx

echo -e "${GREEN}✅ Redeployment complete!${NC}"
sudo systemctl status $SERVICE --no-pager -l
