#!/bin/bash

# ShitCode Error Dashboard - Full Database Reset Script
# This script will completely reset the database and apply all migrations

echo "🔄 Starting full database reset..."

# Database configuration (update these if needed)
DB_NAME="error_log_dashboard"
DB_USER="root"

echo "⚠️  WARNING: This will DELETE all data in the '$DB_NAME' database!"
read -p "Are you sure you want to continue? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "❌ Database reset cancelled"
    exit 1
fi

echo ""
echo "📦 Step 1: Stopping application..."
lsof -ti:5009 | xargs kill -9 2>/dev/null
sleep 2

echo "🗑️  Step 2: Dropping existing database..."
mysql -u $DB_USER -p <<EOF
DROP DATABASE IF EXISTS $DB_NAME;
CREATE DATABASE $DB_NAME CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE $DB_NAME;
SELECT 'Database recreated successfully' as Status;
EOF

if [ $? -ne 0 ]; then
    echo "❌ Failed to recreate database"
    exit 1
fi

echo "✅ Database dropped and recreated"
echo ""
echo "🔧 Step 3: Applying EF Core migrations..."
cd /Users/herralpha/Development/project/ShitCode/ErrorLogDashboard.Web

# Remove old migrations folder to start fresh
rm -rf Migrations

# Create initial migration with all current models
dotnet ef migrations add InitialCreate

if [ $? -ne 0 ]; then
    echo "❌ Failed to create migration"
    exit 1
fi

# Apply migrations to database
dotnet ef database update

if [ $? -ne 0 ]; then
    echo "❌ Failed to apply migrations"
    exit 1
fi

echo "✅ Migrations applied successfully"
echo ""
echo "🚀 Step 4: Starting application (will seed data automatically)..."
dotnet run &

# Wait for application to start
echo "⏳ Waiting for application to start..."
sleep 10

# Check if app is running
if lsof -i:5009 > /dev/null 2>&1; then
    echo ""
    echo "✅ Database reset complete!"
    echo "🌐 Application is running at: http://localhost:5009"
    echo ""
    echo "📊 Seeded data includes:"
    echo "   - 3 Subscription Plans (Free, Pro, Enterprise)"
    echo "   - 4 Test Users (admin@test.com, user@test.com, etc.)"
    echo "   - 1 Sample Project with error logs"
    echo ""
    echo "🔑 Admin Login:"
    echo "   Email: admin@test.com"
    echo "   Password: Admin123!"
else
    echo "⚠️  Application failed to start. Please run manually:"
    echo "   cd /Users/herralpha/Development/project/ShitCode/ErrorLogDashboard.Web"
    echo "   dotnet run"
fi
