#!/bin/bash

# ShitCode Migrate & Redeploy Script
# Applies EF Core migrations and then performs a full deployment.

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}🔄 Starting migration and redeployment...${NC}"

# 1. Apply Migrations
echo -e "${GREEN}🔧 Step 1: Applying EF Core migrations...${NC}"
cd /Users/herralpha/Development/project/ShitCode/ErrorLogDashboard.Web
dotnet ef database update

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Failed to apply migrations. Deployment halted.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Migrations applied successfully.${NC}"

# 2. Call Deployment Script
echo -e "${GREEN}🚀 Step 2: Running full deployment...${NC}"
cd /Users/herralpha/Development/project/ShitCode
./deploy.sh
