-- Manual SQL migration to add Webhook Type column
-- Run this directly in your MySQL database

USE error_log_dashboard;

-- Add Type column to WebhookConfigs table
ALTER TABLE WebhookConfigs 
ADD COLUMN Type INT NOT NULL DEFAULT 0 AFTER SecretToken;

-- Verify the change
DESCRIBE WebhookConfigs;
