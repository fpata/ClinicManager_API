using ClinicManager.Models.Enums;

namespace ClinicManager.Models
{
    public class SearchModel
    {
        public int? UserID { get; set; } 

        public int? PatientID { get; set; }
        // User fields
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public UserType? UserType { get; set; }
        public string? PermCity { get; set; }
        // Contact fields
        public string? PrimaryPhone { get; set; }
        public string? PrimaryEmail { get; set; }

        public int? DoctorID { get; set; } = 0;
        public string? DoctorName { get; set; } = string.Empty;
        // Last treatment info for patients
        public string? LastTreatmentName { get; set; }
        public DateTime? LastTreatmentDate { get; set; }
        
        // Apply date filters only if client provides them
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;

   }


    public class SearchResults
    {
        public int TotalCount { get; set; } =0;
        public bool HasMoreRecords { get; set; }  = false;
        public string? Message { get; set; } =String.Empty;

        public IEnumerable<SearchModel>? Results { get; set; } = null;
    }
}
