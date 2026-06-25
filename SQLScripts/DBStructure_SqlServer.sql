-- ClinicManager API SQL Server Database Structure Initialization Script

-- Drop tables in reverse-dependency order if they exist
IF OBJECT_ID('dbo.payment', 'U') IS NOT NULL DROP TABLE dbo.payment;
IF OBJECT_ID('dbo.billingrecord', 'U') IS NOT NULL DROP TABLE dbo.billingrecord;
IF OBJECT_ID('dbo.patientvitals', 'U') IS NOT NULL DROP TABLE dbo.patientvitals;
IF OBJECT_ID('dbo.patientreport', 'U') IS NOT NULL DROP TABLE dbo.patientreport;
IF OBJECT_ID('dbo.patienttreatmentdetail', 'U') IS NOT NULL DROP TABLE dbo.patienttreatmentdetail;
IF OBJECT_ID('dbo.patienttreatment', 'U') IS NOT NULL DROP TABLE dbo.patienttreatment;
IF OBJECT_ID('dbo.patientappointment', 'U') IS NOT NULL DROP TABLE dbo.patientappointment;
IF OBJECT_ID('dbo.patient', 'U') IS NOT NULL DROP TABLE dbo.patient;
IF OBJECT_ID('dbo.contact', 'U') IS NOT NULL DROP TABLE dbo.contact;
IF OBJECT_ID('dbo.address', 'U') IS NOT NULL DROP TABLE dbo.address;
IF OBJECT_ID('dbo.config', 'U') IS NOT NULL DROP TABLE dbo.config;
IF OBJECT_ID('dbo.roleaccess', 'U') IS NOT NULL DROP TABLE dbo.roleaccess;
IF OBJECT_ID('dbo.user', 'U') IS NOT NULL DROP TABLE dbo.[user];
IF OBJECT_ID('dbo.emailtemplate', 'U') IS NOT NULL DROP TABLE dbo.emailtemplate;


-- 1. Table structure for table user
CREATE TABLE [dbo].[user] (
  [ID] int NOT NULL IDENTITY(1,1),
  [FirstName] nvarchar(50) NOT NULL,
  [MiddleName] nvarchar(50) DEFAULT NULL,
  [LastName] nvarchar(50) NOT NULL,
  [UserName] nvarchar(50) DEFAULT NULL,
  [Password] nvarchar(255) DEFAULT NULL,
  [UserType] int DEFAULT NULL,
  [Age] int DEFAULT NULL,
  [Gender] int DEFAULT NULL,
  [DateOfBirth] datetime2 DEFAULT NULL,
  [LastLoginDate] datetime2 DEFAULT NULL,
  [Designation] nvarchar(100) DEFAULT NULL,
  [LicenseNumber] nvarchar(100) DEFAULT NULL,
  [Specialization] nvarchar(200) DEFAULT NULL,
  [LicenseExpiryDate] datetime2 DEFAULT NULL,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  CONSTRAINT [PK_User] PRIMARY KEY ([ID])
);

-- 2. Table structure for table roleaccess
CREATE TABLE [dbo].[roleaccess] (
  [ID] int NOT NULL IDENTITY(1,1),
  [RoleName] nvarchar(50) NOT NULL,
  [CanAccessPatient] bit NOT NULL DEFAULT 0,
  [CanAccessDashboard] bit NOT NULL DEFAULT 0,
  [CanAccessBilling] bit NOT NULL DEFAULT 0,
  [CanAccessConfig] bit NOT NULL DEFAULT 0,
  [CreatedDate] datetime2 DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  CONSTRAINT [PK_RoleAccess] PRIMARY KEY ([ID]),
  CONSTRAINT [UQ_RoleAccess_RoleName] UNIQUE ([RoleName])
);

-- 3. Table structure for table config
CREATE TABLE [dbo].[config] (
  [ID] int NOT NULL IDENTITY(1,1),
  [CreatedDate] datetime2 DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  [ClinicOpenTime] nvarchar(45) DEFAULT NULL,
  [ClinicEndTime] nvarchar(45) DEFAULT NULL,
  [ClinicName] nvarchar(255) DEFAULT NULL,
  [ClinicProp] nvarchar(255) DEFAULT NULL,
  [PerPatientSlotInMinutes] int DEFAULT NULL,
  [LunchTime] nvarchar(45) DEFAULT NULL,
  [pageSize] int NOT NULL DEFAULT 10,
  [DateFormat] nvarchar(45) DEFAULT NULL,
  [Currency] nvarchar(45) DEFAULT NULL,
  [ClinicLogo] nvarchar(max) DEFAULT NULL,
  [ClinicAddress] nvarchar(1000) DEFAULT NULL,
  CONSTRAINT [PK_Config] PRIMARY KEY ([ID])
);

-- 4. Table structure for table address
CREATE TABLE [dbo].[address] (
  [ID] int NOT NULL IDENTITY(1,1),
  [PermAddress1] nvarchar(200) DEFAULT NULL,
  [PermAddress2] nvarchar(200) DEFAULT NULL,
  [PermState] nvarchar(45) DEFAULT NULL,
  [PermCity] nvarchar(45) DEFAULT NULL,
  [PermCountry] nvarchar(45) DEFAULT NULL,
  [PermZipCode] nvarchar(45) DEFAULT NULL,
  [CorrAddress1] nvarchar(200) DEFAULT NULL,
  [CorrAddress2] nvarchar(200) DEFAULT NULL,
  [CorrState] nvarchar(45) DEFAULT NULL,
  [CorrCity] nvarchar(45) DEFAULT NULL,
  [CorrCountry] nvarchar(45) DEFAULT NULL,
  [CorrZipCode] nvarchar(45) DEFAULT NULL,
  [UserID] int DEFAULT NULL,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  CONSTRAINT [PK_Address] PRIMARY KEY ([ID]),
  CONSTRAINT [UQ_Address_UserID] UNIQUE ([UserID]),
  CONSTRAINT [FK_Address_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[user] ([ID]) ON DELETE SET NULL
);
CREATE INDEX [IX_Address_UserID] ON [dbo].[address] ([UserID]);

-- 5. Table structure for table contact
CREATE TABLE [dbo].[contact] (
  [ID] int NOT NULL IDENTITY(1,1),
  [PrimaryPhone] nvarchar(100) NOT NULL,
  [SecondaryPhone] nvarchar(100) DEFAULT NULL,
  [PrimaryEmail] nvarchar(100) NOT NULL,
  [SecondaryEmail] nvarchar(100) DEFAULT NULL,
  [RelativeName] nvarchar(200) DEFAULT NULL,
  [RelativeRealtion] nvarchar(200) DEFAULT NULL,
  [RelativePhone] nvarchar(200) DEFAULT NULL,
  [RelativeEmail] nvarchar(100) DEFAULT NULL,
  [UserID] int DEFAULT 1,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  CONSTRAINT [PK_Contact] PRIMARY KEY ([ID]),
  CONSTRAINT [UQ_Contact_UserID] UNIQUE ([UserID]),
  CONSTRAINT [FK_Contact_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[user] ([ID]) ON DELETE SET NULL
);
CREATE INDEX [IX_Contact_UserID] ON [dbo].[contact] ([UserID]);

-- 6. Table structure for table patient
CREATE TABLE [dbo].[patient] (
  [ID] int NOT NULL IDENTITY(1,1),
  [UserID] int NOT NULL,
  [Allergies] nvarchar(1000) DEFAULT NULL,
  [Medications] nvarchar(1000) DEFAULT NULL,
  [FatherMedicalHistory] nvarchar(500) DEFAULT NULL,
  [MotherMedicalHistory] nvarchar(500) DEFAULT NULL,
  [PersonalMedicalHistory] nvarchar(500) DEFAULT NULL,
  [InsuranceProvider] nvarchar(50) DEFAULT NULL,
  [InsurancePolicyNumber] nvarchar(100) DEFAULT NULL,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  CONSTRAINT [PK_Patient] PRIMARY KEY ([ID]),
  CONSTRAINT [FK_Patient_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[user] ([ID])
);
CREATE INDEX [IX_Patient_UserID] ON [dbo].[patient] ([UserID]);

-- 7. Table structure for table patientappointment
CREATE TABLE [dbo].[patientappointment] (
  [ID] int NOT NULL IDENTITY(1,1),
  [UserID] int DEFAULT NULL,
  [PatientID] int DEFAULT NULL,
  [PatientName] nvarchar(max) DEFAULT NULL,
  [DoctorID] int DEFAULT NULL,
  [DoctorName] nvarchar(max) DEFAULT NULL,
  [TreatmentName] nvarchar(max) DEFAULT NULL,
  [StartDateTime] datetime2 DEFAULT NULL,
  [EndDateTime] datetime2 DEFAULT NULL,
  [AppointmentStatus] nvarchar(max) DEFAULT NULL,
  [Notes] nvarchar(500) DEFAULT NULL,
  [CheckInTime] datetime2 DEFAULT NULL,
  [CheckOutTime] datetime2 DEFAULT NULL,
  [CancellationReason] nvarchar(200) DEFAULT NULL,
  [ReminderSentDate] datetime2 DEFAULT NULL,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  CONSTRAINT [PK_PatientAppointment] PRIMARY KEY ([ID]),
  CONSTRAINT [FK_PatientAppointment_Patient] FOREIGN KEY ([PatientID]) REFERENCES [dbo].[patient] ([ID]) ON DELETE SET NULL,
  CONSTRAINT [FK_PatientAppointment_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[user] ([ID]) ON DELETE SET NULL
);
CREATE INDEX [IX_PatientAppointment_UserID] ON [dbo].[patientappointment] ([UserID]);
CREATE INDEX [IX_PatientAppointment_PatientID] ON [dbo].[patientappointment] ([PatientID]);

-- 8. Table structure for table patienttreatment
CREATE TABLE [dbo].[patienttreatment] (
  [ID] int NOT NULL IDENTITY(1,1),
  [UserID] int DEFAULT NULL,
  [PatientID] int DEFAULT NULL,
  [DoctorID] int DEFAULT NULL,
  [AppointmentID] int DEFAULT NULL,
  [ChiefComplaint] nvarchar(500) NOT NULL,
  [ClinicalFindings] nvarchar(1000) DEFAULT NULL,
  [Diagnosis] nvarchar(500) DEFAULT NULL,
  [TreatmentPlan] nvarchar(1000) DEFAULT NULL,
  [Prescription] nvarchar(500) DEFAULT NULL,
  [PaymentStatus] nvarchar(50) DEFAULT NULL,
  [EstimatedCost] float DEFAULT NULL,
  [ActualCost] float DEFAULT NULL,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  [BillingRecordID] int DEFAULT NULL,
  CONSTRAINT [PK_PatientTreatment] PRIMARY KEY ([ID]),
  CONSTRAINT [FK_PatientTreatment_Patient] FOREIGN KEY ([PatientID]) REFERENCES [dbo].[patient] ([ID]) ON DELETE SET NULL,
  CONSTRAINT [FK_PatientTreatment_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[user] ([ID]) ON DELETE SET NULL
);
CREATE INDEX [IX_PatientTreatment_UserID] ON [dbo].[patienttreatment] ([UserID]);
CREATE INDEX [IX_PatientTreatment_PatientID] ON [dbo].[patienttreatment] ([PatientID]);

-- 9. Table structure for table patienttreatmentdetail
CREATE TABLE [dbo].[patienttreatmentdetail] (
  [ID] int NOT NULL IDENTITY(1,1),
  [PatientTreatmentID] int DEFAULT NULL,
  [UserID] int DEFAULT NULL,
  [Tooth] nvarchar(max) DEFAULT NULL,
  [Procedure] nvarchar(max) DEFAULT NULL,
  [Prescription] nvarchar(max) DEFAULT NULL,
  [TreatmentDate] datetime2 DEFAULT NULL,
  [PatientID] int DEFAULT NULL,
  [FollowUpInstructions] nvarchar(max) DEFAULT NULL,
  [IsActive] int NOT NULL DEFAULT 1,
  [FollowUpDate] nvarchar(max) DEFAULT NULL,
  [ProcedureTreatmentCost] float DEFAULT 0,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  CONSTRAINT [PK_PatientTreatmentDetail] PRIMARY KEY ([ID]),
  CONSTRAINT [FK_PTD_Patient] FOREIGN KEY ([PatientID]) REFERENCES [dbo].[patient] ([ID]) ON DELETE NO ACTION,
  CONSTRAINT [FK_PTD_Treatment] FOREIGN KEY ([PatientTreatmentID]) REFERENCES [dbo].[patienttreatment] ([ID]) ON DELETE SET NULL,
  CONSTRAINT [FK_PTD_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[user] ([ID]) ON DELETE NO ACTION
);
CREATE INDEX [IX_PTD_PatientTreatmentID] ON [dbo].[patienttreatmentdetail] ([PatientTreatmentID]);
CREATE INDEX [FK_PTD_User] ON [dbo].[patienttreatmentdetail] ([UserID]);
CREATE INDEX [FK_PTD_Patient] ON [dbo].[patienttreatmentdetail] ([PatientID]);

-- 10. Table structure for table patientreport
CREATE TABLE [dbo].[patientreport] (
  [ID] int NOT NULL IDENTITY(1,1),
  [UserID] int DEFAULT NULL,
  [PatientID] int DEFAULT NULL,
  [ReportName] nvarchar(max) DEFAULT NULL,
  [ReportDetails] nvarchar(max) DEFAULT NULL,
  [ReportFilePath] nvarchar(max) DEFAULT NULL,
  [DoctorID] int DEFAULT NULL,
  [DoctorName] nvarchar(max) DEFAULT NULL,
  [ReportDate] datetime2 DEFAULT CURRENT_TIMESTAMP,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  CONSTRAINT [PK_PatientReport] PRIMARY KEY ([ID]),
  CONSTRAINT [FK_PatientReport_Patient] FOREIGN KEY ([PatientID]) REFERENCES [dbo].[patient] ([ID]) ON DELETE SET NULL,
  CONSTRAINT [FK_PatientReport_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[user] ([ID]) ON DELETE SET NULL
);
CREATE INDEX [IX_PatientReport_UserID] ON [dbo].[patientreport] ([UserID]);
CREATE INDEX [IX_PatientReport_PatientID] ON [dbo].[patientreport] ([PatientID]);

-- 11. Table structure for table patientvitals
CREATE TABLE [dbo].[patientvitals] (
  [ID] int NOT NULL IDENTITY(1,1),
  [UserID] int DEFAULT NULL,
  [PatientID] int DEFAULT NULL,
  [RecordedDate] datetime2 DEFAULT CURRENT_TIMESTAMP,
  [BloodPressureSystolic] int DEFAULT NULL,
  [BloodPressureDiastolic] int DEFAULT NULL,
  [HeartRate] int DEFAULT NULL,
  [Temperature] nvarchar(max) DEFAULT NULL,
  [Weight] nvarchar(max) DEFAULT NULL,
  [Height] nvarchar(max) DEFAULT NULL,
  [OxygenSaturation] int DEFAULT NULL,
  [RespiratoryRate] int DEFAULT NULL,
  [Notes] nvarchar(500) DEFAULT NULL,
  [BloodType] nvarchar(100) DEFAULT NULL,
  [SugarFasting] int DEFAULT NULL,
  [SugarRandom] int DEFAULT NULL,
  [SugarPostPrandial] int DEFAULT NULL,
  [RecordedBy] int DEFAULT 1,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  CONSTRAINT [PK_PatientVitals] PRIMARY KEY ([ID]),
  CONSTRAINT [FK_PatientVitals_Patient] FOREIGN KEY ([PatientID]) REFERENCES [dbo].[patient] ([ID]) ON DELETE SET NULL,
  CONSTRAINT [FK_PatientVitals_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[user] ([ID]) ON DELETE SET NULL
);
CREATE INDEX [IX_PatientVitals_UserID] ON [dbo].[patientvitals] ([UserID]);
CREATE INDEX [IX_PatientVitals_PatientID] ON [dbo].[patientvitals] ([PatientID]);

-- 12. Table structure for table billingrecord
CREATE TABLE [dbo].[billingrecord] (
  [ID] int NOT NULL IDENTITY(1,1),
  [TreatmentID] int DEFAULT NULL,
  [PatientID] int DEFAULT NULL,
  [DoctorID] int DEFAULT NULL,
  [PatientName] nvarchar(max) DEFAULT NULL,
  [DoctorName] nvarchar(max) DEFAULT NULL,
  [TreatmentName] nvarchar(max) DEFAULT NULL,
  [ServiceDate] datetime2 DEFAULT NULL,
  [PostedDate] datetime2 DEFAULT NULL,
  [Status] int DEFAULT NULL,
  [Subtotal] float DEFAULT NULL,
  [TaxTotal] float DEFAULT NULL,
  [DiscountTotal] float DEFAULT NULL,
  [Total] float DEFAULT NULL,
  [AmountPaid] float DEFAULT NULL,
  [BalanceDue] float DEFAULT NULL,
  [Notes] nvarchar(max) DEFAULT NULL,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  CONSTRAINT [PK_BillingRecord] PRIMARY KEY ([ID]),
  CONSTRAINT [FK_BillingRecord_Doctor] FOREIGN KEY ([DoctorID]) REFERENCES [dbo].[user] ([ID]) ON DELETE SET NULL,
  CONSTRAINT [FK_BillingRecord_Patient] FOREIGN KEY ([PatientID]) REFERENCES [dbo].[patient] ([ID]) ON DELETE NO ACTION,
  CONSTRAINT [FK_BillingRecord_TreatmentID] FOREIGN KEY ([TreatmentID]) REFERENCES [dbo].[patienttreatment] ([ID])
);
CREATE INDEX [IX_BillingRecord_PatientID] ON [dbo].[billingrecord] ([PatientID]);
CREATE INDEX [IX_BillingRecord_DoctorID] ON [dbo].[billingrecord] ([DoctorID]);
CREATE INDEX [FK_BillingRecord_TreatmentID_idx] ON [dbo].[billingrecord] ([TreatmentID]);

-- 13. Table structure for table payment
CREATE TABLE [dbo].[payment] (
  [ID] int NOT NULL IDENTITY(1,1),
  [BillingID] int DEFAULT NULL,
  [Amount] float DEFAULT NULL,
  [PaymentMethod] int DEFAULT NULL,
  [TransactionDate] nvarchar(max) DEFAULT NULL,
  [Reference] nvarchar(max) DEFAULT NULL,
  [Notes] nvarchar(max) DEFAULT NULL,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  CONSTRAINT [PK_Payment] PRIMARY KEY ([ID]),
  CONSTRAINT [FK_Payment_Billing] FOREIGN KEY ([BillingID]) REFERENCES [dbo].[billingrecord] ([ID]) ON DELETE SET NULL
);
CREATE INDEX [IX_Payment_BillingID] ON [dbo].[payment] ([BillingID]);

-- 14. Table structure for table emailtemplate
CREATE TABLE [dbo].[emailtemplate] (
  [ID] int NOT NULL IDENTITY(1,1),
  [TemplateId] nvarchar(100) NOT NULL,
  [TemplateType] nvarchar(100) NOT NULL DEFAULT 'Email',
  [Subject] nvarchar(200) NULL,
  [HtmlContent] nvarchar(max) NOT NULL,
  [CreatedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [ModifiedDate] datetime2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
  [CreatedBy] int DEFAULT 1,
  [ModifiedBy] int DEFAULT 1,
  [IsActive] tinyint DEFAULT 1,
  CONSTRAINT [PK_EmailTemplate] PRIMARY KEY ([ID]),
  CONSTRAINT [UQ_EmailTemplate_TemplateId] UNIQUE ([TemplateId])
);

