using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BreastCancer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadImagesController : ControllerBase
    {
        // ============================== Upload NationalId For Doctor ==============================

        [HttpPost("uploadNationalIdForDoctor")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadNationalIdForDoctor([FromForm] UploadImageDto file)
        {
            if (file.File == null || file.File.Length == 0)
                return BadRequest("No file provided");

            if (!new[] { "image/jpeg", "image/png", "image/jpg" }.Contains(file.File.ContentType))
                return BadRequest("Invalid file type");

            if (file.File.Length > 5 * 1024 * 1024)
                return BadRequest("Image must be <= 5MB");

            var fileName = await SaveDoctorImageAsync(file.File, "wwwroot/uploads/doctor-ids");

            return Ok(new UploadFileResponseDto
            {
                FilePath = fileName
            });
        }
        private async Task<string> SaveDoctorImageAsync(IFormFile image,string FileLocation)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var path = Path.Combine(FileLocation, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var stram = new FileStream(path, FileMode.Create);
            await image.CopyToAsync(stram);

            return fileName;
        }
    }
}

