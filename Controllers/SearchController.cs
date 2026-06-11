using ClinicManager.DAL;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly ClinicDbContext _context;
        private readonly ILogger<SearchController> _logger;
        private const int CACHE_EXPIRY_MINUTES = 5;

        public SearchController(ClinicDbContext context, ILogger<SearchController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // PSEUDOCODE:
        // 1. Build base query joining users with optional patient, address, contact.
        // 2. Ensure active status filters.
        // 3. Apply conditional filters only when corresponding SearchModel properties have values.
        //    - For UserName filter, avoid CS8602 by adding null check (user.UserName != null && ...)
        // 4. Group results to consolidate duplicates and project to SearchModel.
        // 5. Execute with AsNoTracking and Take(100).
        // 6. Handle exceptions with logging.
        [HttpPost("user")]
        public async Task<ActionResult<IEnumerable<SearchModel>>> SearchUser([FromBody] SearchModel model)
        {
            _logger.LogInformation("Searching users by fields");
            try
            {
                var query = from user in _context.Users
                            join patient in _context.Patients on user.ID equals patient.UserID into patientGroup
                            from patient in patientGroup.DefaultIfEmpty()
                            join address in _context.Addresses on user.ID equals address.UserID into addressGroup
                            from address in addressGroup.DefaultIfEmpty()
                            join contact in _context.Contacts on user.ID equals contact.UserID into contactGroup
                            from contact in contactGroup.DefaultIfEmpty()
                            where user.IsActive == 1 &&
                                  (address == null || address.IsActive == 1) &&
                                  (contact == null || contact.IsActive == 1)
                            select new { user, patient, address, contact };

                // If UserID is provided, search only by UserID
                if (model.UserID.HasValue && model.UserID.Value > 0)
                    query = query.Where(x => x.user.ID == model.UserID.Value);
                else
                {
                    // Apply other filters only if UserID is not provided
                    if (!string.IsNullOrWhiteSpace(model.FirstName))
                        query = query.Where(x => x.user.FirstName != null && x.user.FirstName.Contains(model.FirstName));

                    if (!string.IsNullOrWhiteSpace(model.LastName))
                        query = query.Where(x => x.user.LastName != null && x.user.LastName.Contains(model.LastName));

                    if (!string.IsNullOrWhiteSpace(model.PrimaryEmail))
                        query = query.Where(x => x.contact != null && x.contact.PrimaryEmail != null && x.contact.PrimaryEmail.Contains(model.PrimaryEmail));

                    if (!string.IsNullOrWhiteSpace(model.PrimaryPhone))
                        query = query.Where(x => x.contact != null && x.contact.PrimaryPhone != null && x.contact.PrimaryPhone.Contains(model.PrimaryPhone));

                    if (!string.IsNullOrWhiteSpace(model.PermCity))
                        query = query.Where(x => x.address != null && x.address.PermCity != null && x.address.PermCity.Contains(model.PermCity));

                    if (!string.IsNullOrWhiteSpace(model.UserName))
                        // FIX: Added null check to avoid CS8602 (possible null dereference)
                        query = query.Where(x => x.user.UserName != null && x.user.UserName.Contains(model.UserName));

                    if (model.UserType.HasValue && model.UserType.Value > 0)
                        query = query.Where(x => x.user.UserType == model.UserType);

                    if (model.StartDate.HasValue && model.StartDate.Value < DateTime.Now.Date.AddDays(-10))
                        query = query.Where(x => x.user.CreatedDate >= model.StartDate.Value);

                }

                // Group by user ID only to avoid complex projection issues in EF Core
                var groupedQuery = query
                    .GroupBy(x => x.user.ID)
                    .Select(g => new SearchModel
                    {
                        UserID = g.Key,
                        FirstName = g.Select(x => x.user.FirstName).FirstOrDefault(),
                        LastName = g.Select(x => x.user.LastName).FirstOrDefault(),
                        UserName = g.Select(x => x.user.UserName).FirstOrDefault(),
                        UserType = g.Select(x => x.user.UserType).FirstOrDefault(),
                        PatientID = g.Max(x => x.patient != null ? (int?)x.patient.ID : null),
                        PermCity = g.Select(x => x.address != null ? x.address.PermCity : null).FirstOrDefault(),
                        PrimaryEmail = g.Select(x => x.contact != null ? x.contact.PrimaryEmail : null).FirstOrDefault(),
                        PrimaryPhone = g.Select(x => x.contact != null ? x.contact.PrimaryPhone : null).FirstOrDefault(),
                        DoctorID = 0,
                        DoctorName = string.Empty,
                        StartDate = g.Select(x => x.user.CreatedDate).FirstOrDefault(),
                        EndDate = g.Select(x => x.user.ModifiedDate).FirstOrDefault()
                    });

                // Calculate total count after grouping but before pagination
                var totalCount = await groupedQuery.CountAsync();

                var results = await groupedQuery
                    .AsNoTracking()
                    .OrderBy(x => x.LastName)
                    .ThenBy(x => x.FirstName)
                    .ThenBy(x => x.UserID)
                    .Skip((model.pageNumber - 1) * model.pageSize)
                    .Take(model.pageSize)
                    .ToListAsync()
                    .ConfigureAwait(false);

                var hasMoreRecords = (model.pageNumber * model.pageSize) < totalCount;
                var response = new SearchResults
                {
                    Results = results,
                    TotalCount = totalCount,
                    HasMoreRecords = hasMoreRecords,
                    Message = hasMoreRecords ? "More records available." : "End of records."
                };
                _logger.LogInformation($"Found {results.Count} users matching search criteria out of {totalCount} total");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing user search");
                return StatusCode(500, "An error occurred while searching users");
            }
        }

        [HttpPost("patient")]
        public async Task<ActionResult<IEnumerable<SearchModel>>> SearchPatient([FromBody] SearchModel model)
        {
            _logger.LogInformation("Searching patients by fields");
            try
            {
                // call the other action and safely extract the user list
                var actionResult = await SearchUser(model);
                IEnumerable<SearchModel> users = actionResult.Value ?? Enumerable.Empty<SearchModel>();

                // If Value was null but the controller returned an object wrapper (e.g. SearchResults),
                // try to extract Results from the OkObjectResult fallback.
                if (!users.Any() && actionResult.Result is OkObjectResult okObj && okObj.Value is SearchResults sr)
                    users = sr.Results ?? Enumerable.Empty<SearchModel>();


                // Build a List<int> of non-null UserID values
                var userIds = users
                    .Where(u => u.UserID.HasValue)
                    .Select(u => (int)u.UserID!)
                    .ToList();

                //Select latest PatientTreatment per user (by max Id) for users in the list and active treatments
                var latestPerUser = await _context.PatientTreatments
                    .Where(t => t.UserID.HasValue && userIds.Contains((int)t.UserID!))
                    .GroupBy(t => t.UserID)
                    .Select(g => new
                    {
                        UserID = g.Key!.Value,
                        MaxId = g.Max(x => x.ID)
                    })
                    .ToListAsync()
                    .ConfigureAwait(false);
                    

                // Determine totalCount and pagedResults from the SearchUser response
                int totalCount;
                List<SearchModel> pagedResults;
                var resultObj = actionResult.Result as OkObjectResult;
                if (resultObj?.Value is SearchResults sr2)
                {
                    totalCount = sr2.TotalCount;
                    pagedResults = sr2.Results?.ToList() ?? users.ToList();
                }
                else
                {
                    pagedResults = users.ToList();
                    totalCount = pagedResults.Count;
                }

                // If there are users in paged results, load latest PatientTreatment per user (by max ID)
                var pagedUserIds = pagedResults.Where(r => r.UserID.HasValue).Select(r => r.UserID!.Value).Distinct().ToList();
                if (pagedUserIds.Any())
                {
                    // Filter previously computed latestPerUser to only those in the current page
                    var latestIdsForPaged = latestPerUser.Where(x => pagedUserIds.Contains(x.UserID)).Select(x => x.MaxId).ToList();

                    var latestTreatments = await _context.PatientTreatments
                        .Where(p => latestIdsForPaged.Contains(p.ID) && p.IsActive == 1)
                        .Select(p => new
                        {
                            UserID = p.UserID.HasValue ? p.UserID.Value : 0,
                            PatientID = p.PatientID,
                            ChiefComplaint = p.ChiefComplaint,
                            CreatedDate = p.CreatedDate,
                            Prescription = p.Prescription
                        })
                        .ToListAsync()
                        .ConfigureAwait(false);

                    var latestDict = latestTreatments.ToDictionary(x => x.UserID, x => x);
                    foreach (var res in pagedResults)
                    {
                        if (res.UserID.HasValue && latestDict.TryGetValue(res.UserID.Value, out var lt))
                        {
                            // Map ChiefComplaint to LastTreatmentName and CreatedDate to LastTreatmentDate
                            res.LastTreatmentName = lt.ChiefComplaint;
                            res.LastTreatmentDate = lt.CreatedDate;
                        }
                    }
                }
                var results = pagedResults;
                var hasMoreRecords = (model.pageNumber * model.pageSize) < totalCount;
                var response = new SearchResults
                {
                    Results = results,
                    TotalCount = totalCount,
                    HasMoreRecords = hasMoreRecords,
                    Message = hasMoreRecords ? "More records available." : "End of records."
                };
                _logger.LogInformation($"Found {results.Count} users matching search criteria out of {totalCount} total");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing user search");
                return StatusCode(500, "An error occurred while searching users");
            }
        }

        // PSEUDOCODE:
        // 1. Start from Users AsNoTracking.
        // 2. Apply filters when model properties have values.
        // 3. For UserName filter add null check to prevent CS8602.
        // 4. Ensure active users only.
        // 5. Project to SearchModel, order, limit, execute.
        // 6. Handle exceptions with logging.
        [HttpPost("advanced")]
        public async Task<ActionResult<IEnumerable<SearchModel>>> AdvancedSearch([FromBody] SearchModel model)
        {
            _logger.LogInformation("Performing advanced search");

            try
            {
                var query = _context.Users.AsNoTracking();

                if (!string.IsNullOrEmpty(model.UserName))
                    // FIX: Added null check to avoid CS8602
                    query = query.Where(u => u.UserName != null && u.UserName.Contains(model.UserName));

                if (!string.IsNullOrEmpty(model.FirstName))
                    query = query.Where(u => u.FirstName != null && u.FirstName.Contains(model.FirstName));

                if (!string.IsNullOrEmpty(model.LastName))
                    query = query.Where(u => u.LastName != null && u.LastName.Contains(model.LastName));

                if (model.UserType.HasValue && model.UserType.Value > 0)
                    query = query.Where(u => u.UserType == model.UserType);

                query = query.Where(u => u.IsActive == 1);

                var results = await query
                    .Select(u => new SearchModel
                    {
                        UserID = u.ID,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        UserName = u.UserName,
                        UserType = u.UserType,
                        StartDate = u.CreatedDate,
                        EndDate = u.ModifiedDate
                    })
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Take(100)
                    .ToListAsync()
                    .ConfigureAwait(false);

                _logger.LogInformation($"Advanced search found {results.Count} results");
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing advanced search");
                return StatusCode(500, "An error occurred while performing advanced search");
            }
        }
    }
}
