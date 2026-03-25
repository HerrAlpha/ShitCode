#!/bin/bash

# Script to setup SSL with Certbot for Nginx
# Usage: sudo ./setup-ssl.sh yourdomain.com

if [ -z "$1" ]; then
    echo "Usage: sudo ./setup-ssl.sh <domain_name>"
    exit 1
fi

DOMAIN=$1

echo "🔒 Installing Certbot and Nginx plugin..."
sudo apt update
sudo apt install -y certbot python3-certbot-nginx

echo "🔒 Obtaining SSL certificate for $DOMAIN..."
sudo certbot --nginx -d $DOMAIN

echo "✅ SSL setup complete for $DOMAIN"
echo "🔄 Nginx should have been reloaded automatically by Certbot"
