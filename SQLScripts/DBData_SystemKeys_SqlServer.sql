-- Seed script for system keys in SQL Server
-- You can replace 'YOUR_...' with your actual credentials.
-- If stored as plain text, they will still work due to the automated plain-text fallback.
-- If using encryption, encrypt them with EncryptionHelper.Encrypt(plainText) before inserting.

MERGE [dbo].[systemkey] AS target
USING (VALUES
  ('EmailSettings:Password', 'YOUR_SMTP_OR_GMAIL_PASSWORD'),
  ('EmailSettings:ApiKey', 'YOUR_SENDGRID_API_KEY'),
  ('Msg91Settings:AuthKey', 'YOUR_MSG91_AUTH_KEY'),
  ('SmsSettings:AccountSid', 'YOUR_TWILIO_SMS_ACCOUNT_SID'),
  ('SmsSettings:AuthToken', 'YOUR_TWILIO_SMS_AUTH_TOKEN'),
  ('WhatsAppSettings:Twilio:AccountSid', 'YOUR_TWILIO_WHATSAPP_ACCOUNT_SID'),
  ('WhatsAppSettings:Twilio:AuthToken', 'YOUR_TWILIO_WHATSAPP_AUTH_TOKEN'),
  ('WhatsAppSettings:Msg91:AuthKey', 'YOUR_MSG91_WHATSAPP_AUTH_KEY')
) AS source (KeyName, KeyValue)
ON (target.KeyName = source.KeyName)
WHEN MATCHED THEN
  UPDATE SET KeyValue = source.KeyValue, ModifiedDate = CURRENT_TIMESTAMP
WHEN NOT MATCHED THEN
  INSERT (KeyName, KeyValue, IsActive)
  VALUES (source.KeyName, source.KeyValue, 1);
