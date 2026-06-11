namespace ClinicManager.Models.Enums
{
    public enum AppointmentStatus
    {
        Scheduled = 1,
        Confirmed = 2,
        InProgress = 3,
        Completed = 4,
        Cancelled = 5,
        NoShow = 6,
        Rescheduled = 7
    }

    public enum UserType
    {
        Patient = 1,
        Doctor = 2,
        Nurse = 3,
        Receptionist = 4,
        Administrator = 5,
        Technician = 6,
        Accountant = 7,
        DentalAssistant = 8
    }

    public enum Gender
    {
        Male = 1,
        Female = 2,
        Other = 3,
        PreferNotToSay = 4
    }
    public enum InsuranceStatus
    {
        Pending = 1,
        Submitted = 2,
        Denied = 3,
        Paid = 4,
        Partial = 5
    }

    public enum BillingStatus
    {
        Draft = 1,
        Submitted = 2,
        PartiallyPaid = 3,
        Paid = 4,
        Voided = 5,
        Adjusted = 6
    }

    public enum PaymentMethod
    {
        Cash = 1,
        CreditCard = 2,
        DebitCard = 3,
        Check = 4,
        BankTransfer = 5,
        Insurance = 6,
        Adjustment = 7,
        WriteOff = 8
    }
}