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
