-- ClinicManager API SQL Server Initial Data Script

-- Ensure IDENTITY_INSERT is enabled for inserting explicit values into the identity column
SET IDENTITY_INSERT [dbo].[roleaccess] ON;

INSERT INTO [dbo].[roleaccess] (
  [ID], 
  [RoleName], 
  [CanAccessPatient], 
  [CanAccessDashboard], 
  [CanAccessBilling], 
  [CanAccessConfig], 
  [CreatedDate], 
  [ModifiedDate], 
  [CreatedBy], 
  [ModifiedBy], 
  [IsActive]
) VALUES 
(1, 'Administrator', 1, 1, 1, 1, '2026-06-01 19:31:33', '2026-06-01 19:31:33', 1, 1, 1),
(2, 'Doctor', 1, 1, 1, 1, '2026-06-01 19:31:33', '2026-06-01 19:31:33', 1, 1, 1),
(3, 'Nurse', 1, 1, 0, 0, '2026-06-01 19:31:33', '2026-06-01 19:31:33', 1, 1, 1),
(4, 'Patient', 1, 0, 0, 0, '2026-06-01 19:31:33', '2026-06-01 19:31:33', 1, 1, 1),
(5, 'Accountant', 1, 0, 1, 0, '2026-06-01 19:31:33', '2026-06-01 19:31:33', 1, 1, 1);

SET IDENTITY_INSERT [dbo].[roleaccess] OFF;

-- Seed default templates for SQL Server
SET IDENTITY_INSERT [dbo].[emailtemplate] ON;

INSERT INTO [dbo].[emailtemplate] (
  [ID],
  [TemplateId],
  [Subject],
  [HtmlContent],
  [CreatedDate],
  [ModifiedDate],
  [CreatedBy],
  [ModifiedBy],
  [IsActive]
) VALUES 
(1, 'ForgotPassword', 'Reset Your Password - {{clinic_name}}', '<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>Reset Your Password</title>
    <style>
        body { font-family: "Helvetica Neue", Helvetica, Arial, sans-serif; background-color: #f7fafc; color: #2d3748; padding: 20px; margin: 0; }
        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0; }
        .header { background: linear-gradient(135deg, #1a365d, #2a4365); padding: 30px; text-align: center; color: #ffffff; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 700; letter-spacing: -0.5px; }
        .content { padding: 40px 30px; line-height: 1.6; }
        .content p { margin: 0 0 20px 0; }
        .password-box { background-color: #ebf8ff; border: 1px solid #bee3f8; border-radius: 6px; padding: 15px; text-align: center; font-size: 22px; font-weight: bold; color: #2b6cb0; letter-spacing: 2px; margin: 30px 0; }
        .footer { background-color: #edf2f7; padding: 20px; text-align: center; font-size: 12px; color: #718096; border-top: 1px solid #e2e8f0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>{{clinic_name}}</h1>
        </div>
        <div class="content">
            <p>Hello {{user_name}},</p>
            <p>We received a request to reset your password for your <strong>{{clinic_name}}</strong> account. Please use the temporary password below to log in:</p>
            <div class="password-box">
                {{new_password}}
            </div>
            <p>For security reasons, we strongly recommend that you change this password immediately after logging in.</p>
            <p>If you did not request a password reset, please ignore this email or contact us if you have any questions.</p>
            <p>Best regards,<br>The {{clinic_name}} Team</p>
        </div>
        <div class="footer">
            &copy; 2026 {{clinic_name}}. All rights reserved.
        </div>
    </div>
</body>
</html>', '2026-06-13 00:00:00', '2026-06-13 00:00:00', 1, 1, 1),

(2, 'AppointmentCreated', 'Appointment Confirmed - {{clinic_name}}', '<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>Appointment Confirmed</title>
    <style>
        body { font-family: "Helvetica Neue", Helvetica, Arial, sans-serif; background-color: #f7fafc; color: #2d3748; padding: 20px; margin: 0; }
        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0; }
        .header { background: linear-gradient(135deg, #2c7a7b, #234e52); padding: 30px; text-align: center; color: #ffffff; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 700; letter-spacing: -0.5px; }
        .content { padding: 40px 30px; line-height: 1.6; }
        .content p { margin: 0 0 20px 0; }
        .details-card { background-color: #f0fff4; border: 1px solid #c6f6d5; border-radius: 6px; padding: 20px; margin: 30px 0; }
        .details-row { display: flex; margin-bottom: 10px; border-bottom: 1px dashed #e2e8f0; padding-bottom: 10px; }
        .details-row:last-child { margin-bottom: 0; border-bottom: none; padding-bottom: 0; }
        .details-label { font-weight: bold; color: #22543d; width: 120px; flex-shrink: 0; }
        .details-value { color: #2d3748; }
        .footer { background-color: #edf2f7; padding: 20px; text-align: center; font-size: 12px; color: #718096; border-top: 1px solid #e2e8f0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Appointment Confirmed</h1>
        </div>
        <div class="content">
            <p>Hello {{user_name}},</p>
            <p>Your appointment has been successfully scheduled at <strong>{{clinic_name}}</strong>. Here are the details of your visit:</p>
            <div class="details-card">
                <div class="details-row">
                    <span class="details-label">Clinic:</span>
                    <span class="details-value">{{clinic_name}}</span>
                </div>
                <div class="details-row">
                    <span class="details-label">Date:</span>
                    <span class="details-value">{{Appointment_Date}}</span>
                </div>
                <div class="details-row">
                    <span class="details-label">Time:</span>
                    <span class="details-value">{{Appointment_Time}}</span>
                </div>
            </div>
            <p>If you need to reschedule or cancel your appointment, please contact us at least 24 hours in advance.</p>
            <p>We look forward to seeing you!</p>
            <p>Best regards,<br>The {{clinic_name}} Team</p>
        </div>
        <div class="footer">
            &copy; 2026 {{clinic_name}}. All rights reserved.
        </div>
    </div>
</body>
</html>', '2026-06-13 00:00:00', '2026-06-13 00:00:00', 1, 1, 1),

(3, 'AppointmentReminder', 'Appointment Reminder - {{clinic_name}}', '<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>Appointment Reminder</title>
    <style>
        body { font-family: "Helvetica Neue", Helvetica, Arial, sans-serif; background-color: #f7fafc; color: #2d3748; padding: 20px; margin: 0; }
        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0; }
        .header { background: linear-gradient(135deg, #3182ce, #2b6cb0); padding: 30px; text-align: center; color: #ffffff; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 700; letter-spacing: -0.5px; }
        .content { padding: 40px 30px; line-height: 1.6; }
        .content p { margin: 0 0 20px 0; }
        .details-card { background-color: #ebf8ff; border: 1px solid #bee3f8; border-radius: 6px; padding: 20px; margin: 30px 0; }
        .details-row { display: flex; margin-bottom: 10px; border-bottom: 1px dashed #e2e8f0; padding-bottom: 10px; }
        .details-row:last-child { margin-bottom: 0; border-bottom: none; padding-bottom: 0; }
        .details-label { font-weight: bold; color: #2b6cb0; width: 120px; flex-shrink: 0; }
        .details-value { color: #2d3748; }
        .footer { background-color: #edf2f7; padding: 20px; text-align: center; font-size: 12px; color: #718096; border-top: 1px solid #e2e8f0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Appointment Reminder</h1>
        </div>
        <div class="content">
            <p>Hello {{user_name}},</p>
            <p>This is a friendly reminder of your upcoming appointment at <strong>{{clinic_name}}</strong>. We look forward to your visit!</p>
            <div class="details-card">
                <div class="details-row">
                    <span class="details-label">Clinic:</span>
                    <span class="details-value">{{clinic_name}}</span>
                </div>
                <div class="details-row">
                    <span class="details-label">Date:</span>
                    <span class="details-value">{{Appointment_Date}}</span>
                </div>
                <div class="details-row">
                    <span class="details-label">Time:</span>
                    <span class="details-value">{{Appointment_Time}}</span>
                </div>
            </div>
            <p>If you have any questions or need to make changes to your scheduled time, please contact us as soon as possible.</p>
            <p>See you soon!</p>
            <p>Best regards,<br>The {{clinic_name}} Team</p>
        </div>
        <div class="footer">
            &copy; 2026 {{clinic_name}}. All rights reserved.
        </div>
    </div>
</body>
</html>', '2026-06-13 00:00:00', '2026-06-13 00:00:00', 1, 1, 1);

SET IDENTITY_INSERT [dbo].[emailtemplate] OFF;

