using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ClinicManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [RequestSizeLimit(20_000_000)] // Limit to ~10MB
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string description)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new
                {
                    message = "File uploaded successfully.",
                    ReportFilePath = filePath,
                    description
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "File upload failed.", error = ex.Message });
            }
        }
    }
}
