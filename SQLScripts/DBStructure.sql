-- MySQL dump 10.13  Distrib 8.0.45, for Win64 (x86_64)
--
-- Host: localhost    Database: reliefdentalclinic
-- ------------------------------------------------------
-- Server version	8.4.8

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `address`
--

DROP TABLE IF EXISTS `address`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `address` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `PermAddress1` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `PermAddress2` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `PermState` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `PermCity` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `PermCountry` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `PermZipCode` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CorrAddress1` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CorrAddress2` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CorrState` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CorrCity` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CorrCountry` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CorrZipCode` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `UserID` int DEFAULT NULL,
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  PRIMARY KEY (`ID`),
  UNIQUE KEY `UserID_UNIQUE` (`UserID`),
  KEY `IX_Address_UserID` (`UserID`),
  CONSTRAINT `FK_Address_User` FOREIGN KEY (`UserID`) REFERENCES `user` (`ID`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=5027 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `billingrecord`
--

DROP TABLE IF EXISTS `billingrecord`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `billingrecord` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `TreatmentID` int DEFAULT NULL,
  `PatientID` int DEFAULT NULL,
  `DoctorID` int DEFAULT NULL,
  `PatientName` text COLLATE utf8mb4_unicode_ci,
  `DoctorName` text COLLATE utf8mb4_unicode_ci,
  `TreatmentName` text COLLATE utf8mb4_unicode_ci,
  `ServiceDate` datetime DEFAULT NULL,
  `PostedDate` datetime DEFAULT NULL,
  `Status` int DEFAULT NULL,
  `Subtotal` float DEFAULT NULL,
  `TaxTotal` float DEFAULT NULL,
  `DiscountTotal` float DEFAULT NULL,
  `Total` float DEFAULT NULL,
  `AmountPaid` float DEFAULT NULL,
  `BalanceDue` float DEFAULT NULL,
  `Notes` text COLLATE utf8mb4_unicode_ci,
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  PRIMARY KEY (`ID`),
  KEY `IX_BillingRecord_PatientID` (`PatientID`),
  KEY `IX_BillingRecord_DoctorID` (`DoctorID`),
  KEY `FK_BillingRecord_TreatmentID_idx` (`TreatmentID`),
  CONSTRAINT `FK_BillingRecord_Doctor` FOREIGN KEY (`DoctorID`) REFERENCES `user` (`ID`) ON DELETE SET NULL,
  CONSTRAINT `FK_BillingRecord_Patient` FOREIGN KEY (`PatientID`) REFERENCES `patient` (`ID`) ON DELETE SET NULL,
  CONSTRAINT `FK_BillingRecord_TreatmentID` FOREIGN KEY (`TreatmentID`) REFERENCES `patienttreatment` (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=5005 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `config`
--

DROP TABLE IF EXISTS `config`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `config` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `CreatedDate` datetime DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  `ClinicOpenTime` varchar(45) DEFAULT NULL,
  `ClinicEndTime` varchar(45) DEFAULT NULL,
  `ClinicName` varchar(255) DEFAULT NULL,
  `ClinicProp` varchar(255) DEFAULT NULL,
  `PerPatientSlotInMinutes` int DEFAULT NULL,
  `LunchTime` varchar(45) DEFAULT NULL,
  `pageSize` int NOT NULL DEFAULT '10',
  `DateFormat` varchar(45) DEFAULT NULL,
  `Currency` varchar(45) DEFAULT NULL,
  `ClinicLogo` longtext,
  `ClinicAddress` varchar(1000) DEFAULT NULL,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `contact`
--

DROP TABLE IF EXISTS `contact`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `contact` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `PrimaryPhone` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `SecondaryPhone` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `PrimaryEmail` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `SecondaryEmail` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `RelativeName` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `RelativeRealtion` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `RelativePhone` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `RelativeEmail` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `UserID` int DEFAULT '1',
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  PRIMARY KEY (`ID`),
  UNIQUE KEY `UserID_UNIQUE` (`UserID`),
  KEY `IX_Contact_UserID` (`UserID`),
  CONSTRAINT `FK_Contact_User` FOREIGN KEY (`UserID`) REFERENCES `user` (`ID`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=5027 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `patient`
--

DROP TABLE IF EXISTS `patient`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `patient` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `UserID` int NOT NULL,
  `Allergies` varchar(1000) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Medications` varchar(1000) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `FatherMedicalHistory` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `MotherMedicalHistory` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `PersonalMedicalHistory` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `InsuranceProvider` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `InsurancePolicyNumber` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  PRIMARY KEY (`ID`),
  KEY `IX_Patient_UserID` (`UserID`),
  CONSTRAINT `FK_Patient_User` FOREIGN KEY (`UserID`) REFERENCES `user` (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=5015 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `patientappointment`
--

DROP TABLE IF EXISTS `patientappointment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `patientappointment` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `UserID` int DEFAULT NULL,
  `PatientID` int DEFAULT NULL,
  `PatientName` text COLLATE utf8mb4_unicode_ci,
  `DoctorID` int DEFAULT NULL,
  `DoctorName` text COLLATE utf8mb4_unicode_ci,
  `TreatmentName` text COLLATE utf8mb4_unicode_ci,
  `StartDateTime` datetime DEFAULT NULL,
  `EndDateTime` datetime DEFAULT NULL,
  `AppointmentStatus` text COLLATE utf8mb4_unicode_ci,
  `Notes` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CheckInTime` datetime DEFAULT NULL,
  `CheckOutTime` datetime DEFAULT NULL,
  `CancellationReason` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ReminderSentDate` datetime DEFAULT NULL,
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  PRIMARY KEY (`ID`),
  KEY `IX_PatientAppointment_UserID` (`UserID`),
  KEY `IX_PatientAppointment_PatientID` (`PatientID`),
  CONSTRAINT `FK_PatientAppointment_Patient` FOREIGN KEY (`PatientID`) REFERENCES `patient` (`ID`) ON DELETE SET NULL,
  CONSTRAINT `FK_PatientAppointment_User` FOREIGN KEY (`UserID`) REFERENCES `user` (`ID`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=5038 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `patientreport`
--

DROP TABLE IF EXISTS `patientreport`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `patientreport` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `UserID` int DEFAULT NULL,
  `PatientID` int DEFAULT NULL,
  `ReportName` text COLLATE utf8mb4_unicode_ci,
  `ReportDetails` text COLLATE utf8mb4_unicode_ci,
  `ReportFilePath` text COLLATE utf8mb4_unicode_ci,
  `DoctorID` int DEFAULT NULL,
  `DoctorName` text COLLATE utf8mb4_unicode_ci,
  `ReportDate` datetime DEFAULT CURRENT_TIMESTAMP,
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  PRIMARY KEY (`ID`),
  KEY `IX_PatientReport_UserID` (`UserID`),
  KEY `IX_PatientReport_PatientID` (`PatientID`),
  CONSTRAINT `FK_PatientReport_Patient` FOREIGN KEY (`PatientID`) REFERENCES `patient` (`ID`) ON DELETE SET NULL,
  CONSTRAINT `FK_PatientReport_User` FOREIGN KEY (`UserID`) REFERENCES `user` (`ID`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=5017 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `patienttreatment`
--

DROP TABLE IF EXISTS `patienttreatment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `patienttreatment` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `UserID` int DEFAULT NULL,
  `PatientID` int DEFAULT NULL,
  `DoctorID` int DEFAULT NULL,
  `AppointmentID` int DEFAULT NULL,
  `ChiefComplaint` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ClinicalFindings` varchar(1000) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Diagnosis` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `TreatmentPlan` varchar(1000) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Prescription` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `PaymentStatus` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `EstimatedCost` float DEFAULT NULL,
  `ActualCost` float DEFAULT NULL,
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  `BillingRecordID` int DEFAULT NULL,
  PRIMARY KEY (`ID`),
  KEY `IX_PatientTreatment_UserID` (`UserID`),
  KEY `IX_PatientTreatment_PatientID` (`PatientID`),
  CONSTRAINT `FK_PatientTreatment_Patient` FOREIGN KEY (`PatientID`) REFERENCES `patient` (`ID`) ON DELETE SET NULL,
  CONSTRAINT `FK_PatientTreatment_User` FOREIGN KEY (`UserID`) REFERENCES `user` (`ID`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=5015 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `patienttreatmentdetail`
--

DROP TABLE IF EXISTS `patienttreatmentdetail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `patienttreatmentdetail` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `PatientTreatmentID` int DEFAULT NULL,
  `UserID` int DEFAULT NULL,
  `Tooth` text COLLATE utf8mb4_unicode_ci,
  `Procedure` text COLLATE utf8mb4_unicode_ci,
  `Prescription` text COLLATE utf8mb4_unicode_ci,
  `TreatmentDate` datetime DEFAULT NULL,
  `PatientID` int DEFAULT NULL,
  `FollowUpInstructions` text COLLATE utf8mb4_unicode_ci,
  `IsActive` int NOT NULL DEFAULT '1',
  `FollowUpDate` text COLLATE utf8mb4_unicode_ci,
  `ProcedureTreatmentCost` float DEFAULT '0',
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  PRIMARY KEY (`ID`),
  KEY `IX_PTD_PatientTreatmentID` (`PatientTreatmentID`),
  KEY `FK_PTD_User` (`UserID`),
  KEY `FK_PTD_Patient` (`PatientID`),
  CONSTRAINT `FK_PTD_Patient` FOREIGN KEY (`PatientID`) REFERENCES `patient` (`ID`) ON DELETE SET NULL,
  CONSTRAINT `FK_PTD_Treatment` FOREIGN KEY (`PatientTreatmentID`) REFERENCES `patienttreatment` (`ID`) ON DELETE SET NULL,
  CONSTRAINT `FK_PTD_User` FOREIGN KEY (`UserID`) REFERENCES `user` (`ID`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=7018 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `patientvitals`
--

DROP TABLE IF EXISTS `patientvitals`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `patientvitals` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `UserID` int DEFAULT NULL,
  `PatientID` int DEFAULT NULL,
  `RecordedDate` datetime DEFAULT CURRENT_TIMESTAMP,
  `BloodPressureSystolic` int DEFAULT NULL,
  `BloodPressureDiastolic` int DEFAULT NULL,
  `HeartRate` int DEFAULT NULL,
  `Temperature` text COLLATE utf8mb4_unicode_ci,
  `Weight` text COLLATE utf8mb4_unicode_ci,
  `Height` text COLLATE utf8mb4_unicode_ci,
  `OxygenSaturation` int DEFAULT NULL,
  `RespiratoryRate` int DEFAULT NULL,
  `Notes` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `BloodType` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `SugarFasting` int DEFAULT NULL,
  `SugarRandom` int DEFAULT NULL,
  `SugarPostPrandial` int DEFAULT NULL,
  `RecordedBy` int DEFAULT '1',
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  PRIMARY KEY (`ID`),
  KEY `IX_PatientVitals_UserID` (`UserID`),
  KEY `IX_PatientVitals_PatientID` (`PatientID`),
  CONSTRAINT `FK_PatientVitals_Patient` FOREIGN KEY (`PatientID`) REFERENCES `patient` (`ID`) ON DELETE SET NULL,
  CONSTRAINT `FK_PatientVitals_User` FOREIGN KEY (`UserID`) REFERENCES `user` (`ID`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=5015 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `payment`
--

DROP TABLE IF EXISTS `payment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `payment` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `BillingID` int DEFAULT NULL,
  `Amount` float DEFAULT NULL,
  `PaymentMethod` int DEFAULT NULL,
  `TransactionDate` text COLLATE utf8mb4_unicode_ci,
  `Reference` text COLLATE utf8mb4_unicode_ci,
  `Notes` text COLLATE utf8mb4_unicode_ci,
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  PRIMARY KEY (`ID`),
  KEY `IX_Payment_BillingID` (`BillingID`),
  CONSTRAINT `FK_Payment_Billing` FOREIGN KEY (`BillingID`) REFERENCES `billingrecord` (`ID`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=55010 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `roleaccess`
--

DROP TABLE IF EXISTS `roleaccess`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roleaccess` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `RoleName` varchar(50) NOT NULL,
  `CanAccessPatient` tinyint(1) NOT NULL DEFAULT '0',
  `CanAccessDashboard` tinyint(1) NOT NULL DEFAULT '0',
  `CanAccessBilling` tinyint(1) NOT NULL DEFAULT '0',
  `CanAccessConfig` tinyint(1) NOT NULL DEFAULT '0',
  `CreatedDate` datetime DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  PRIMARY KEY (`ID`),
  UNIQUE KEY `RoleName` (`RoleName`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `user`
--

DROP TABLE IF EXISTS `user`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user` (
  `ID` int NOT NULL AUTO_INCREMENT,
  `FirstName` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `MiddleName` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `LastName` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `UserName` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Password` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `UserType` int DEFAULT NULL,
  `Age` int DEFAULT NULL,
  `Gender` int DEFAULT NULL,
  `DateOfBirth` datetime DEFAULT NULL,
  `LastLoginDate` datetime DEFAULT NULL,
  `Designation` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `LicenseNumber` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Specialization` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `LicenseExpiryDate` datetime DEFAULT NULL,
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `CreatedBy` int DEFAULT '1',
  `ModifiedBy` int DEFAULT '1',
  `IsActive` tinyint DEFAULT '1',
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB AUTO_INCREMENT=5017 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-06-09 13:14:00
