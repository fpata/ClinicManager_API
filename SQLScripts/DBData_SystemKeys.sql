-- Seed script for system keys in MySQL/MariaDB
-- You can replace 'YOUR_...' with your actual credentials.
-- If stored as plain text, they will still work due to the automated plain-text fallback.
-- If using encryption, encrypt them with EncryptionHelper.Encrypt(plainText) before inserting.

INSERT INTO `systemkey` (`KeyName`, `KeyValue`, `IsActive`) VALUES
('EmailSettings:Password', 'YOUR_SMTP_OR_GMAIL_PASSWORD', 1),
('EmailSettings:ApiKey', 'YOUR_SENDGRID_API_KEY', 1),
('Msg91Settings:AuthKey', 'YOUR_MSG91_AUTH_KEY', 1),
('SmsSettings:AccountSid', 'YOUR_TWILIO_SMS_ACCOUNT_SID', 1),
('SmsSettings:AuthToken', 'YOUR_TWILIO_SMS_AUTH_TOKEN', 1),
('WhatsAppSettings:Twilio:AccountSid', 'YOUR_TWILIO_WHATSAPP_ACCOUNT_SID', 1),
('WhatsAppSettings:Twilio:AuthToken', 'YOUR_TWILIO_WHATSAPP_AUTH_TOKEN', 1),
('WhatsAppSettings:Msg91:AuthKey', 'YOUR_MSG91_WHATSAPP_AUTH_KEY', 1)
ON DUPLICATE KEY UPDATE `KeyValue` = VALUES(`KeyValue`);
