using BreastCancer.DTO.request;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BreastCancer.Controllers
{
    /// <summary>
    /// Controller for managing doctor operations
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Doctor")]
    public class DoctorController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        private readonly ILogger<DoctorController> _logger;

        public DoctorController(IDoctorService doctorService, ILogger<DoctorController> logger)
        {
            _doctorService = doctorService;
            _logger = logger;
        }

        /// <summary>
        /// Get all doctors with pagination
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1, minimum: 1)</param>
        /// <param name="pageSize">Page size (default: 10, range: 1-100)</param>
        /// <returns>List of doctors with pagination</returns>
        /// <remarks>
        /// Requires Admin role.
        /// Returns a paginated list of all doctors in the system.
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllDoctors([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var doctors = await _doctorService.GetAllDoctorsAsync(pageNumber, pageSize);
                return Ok(doctors);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while retrieving all doctors");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all doctors");
                return BadRequest(new { message = "An error occurred while retrieving doctors." });
            }
        }

        /// <summary>
        /// Get a doctor by UserId
        /// </summary>
        /// <param name="id">Doctor UserId (primary key)</param>
        /// <returns>Doctor details</returns>
        /// <remarks>
        /// Requires one of the following roles: Doctor, Admin, Patient, or Caregiver.
        /// The ID parameter refers to the UserId which is the primary key of the Doctor entity.
        /// </remarks>
        [HttpGet("{id}")]
        [Authorize(Roles = "Doctor, Admin, Patient, Caregiver")]
        public async Task<IActionResult> GetDoctorById(string id)
        {
            try
            {
                var doctor = await _doctorService.GetDoctorByIdAsync(id);
                if (doctor == null)
                {
                    return NotFound(new { message = $"Doctor with UserId '{id}' not found." });
                }

                return Ok(doctor);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while retrieving doctor with UserId: {UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor with UserId: {UserId}", id);
                return BadRequest(new { message = "An error occurred while retrieving the doctor." });
            }
        }

        /// <summary>
        /// Create a new doctor
        /// </summary>
        /// <param name="doctorDto">Doctor creation data</param>
        /// <returns>Created doctor with generated UserId</returns>
        /// <remarks>
        /// Requires Admin role.
        /// Creates both an ApplicationUser and a Doctor entity.
        /// A temporary password will be automatically generated for the doctor.
        /// The returned ID is the UserId which serves as the Doctor's primary key.
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateDoctor([FromBody] DoctorCreateDTO doctorDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdDoctor = await _doctorService.CreateDoctorAsync(doctorDto);
                return CreatedAtAction(
                    nameof(GetDoctorById),
                    new { id = createdDoctor.Id },
                    createdDoctor);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while creating doctor");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating doctor");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating doctor");
                return BadRequest(new { message = "An error occurred while creating the doctor." });
            }
        }

        /// <summary>
        /// Update an existing doctor
        /// </summary>
        /// <param name="id">Doctor UserId (primary key)</param>
        /// <param name="doctorDto">Doctor update data</param>
        /// <returns>Updated doctor</returns>
        /// <remarks>
        /// Requires Admin or Doctor role.
        /// Doctors can only update their own information.
        /// The ID parameter refers to the UserId which is the primary key of the Doctor entity.
        /// </remarks>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin, Doctor")]
        public async Task<IActionResult> UpdateDoctor(string id, [FromBody] DoctorUpdateDTO doctorDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedDoctor = await _doctorService.UpdateDoctorAsync(id, doctorDto);
                if (updatedDoctor == null)
                {
                    return NotFound(new { message = $"Doctor with UserId '{id}' not found." });
                }

                return Ok(updatedDoctor);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while updating doctor with UserId: {UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating doctor with UserId: {UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor with UserId: {UserId}", id);
                return BadRequest(new { message = "An error occurred while updating the doctor." });
            }
        }

        /// <summary>
        /// Delete a doctor (soft delete - sets IsActive to false)
        /// </summary>
        /// <param name="id">Doctor UserId (primary key)</param>
        /// <returns>No content on success</returns>
        /// <remarks>
        /// Requires Admin or Doctor role.
        /// Doctors can only delete their own account.
        /// This is a soft delete operation that sets the IsActive flag to false.
        /// The ID parameter refers to the UserId which is the primary key of the Doctor entity.
        /// </remarks>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin, Doctor")]
        public async Task<IActionResult> DeleteDoctor(string id)
        {
            try
            {
                await _doctorService.DeleteDoctorAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while deleting doctor with UserId: {UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while deleting doctor with UserId: {UserId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting doctor with UserId: {UserId}", id);
                return BadRequest(new { message = "An error occurred while deleting the doctor." });
            }
        }

        /// <summary>
        /// Hard delete a doctor (permanently remove from database)
        /// </summary>
        /// <param name="id">Doctor UserId (primary key)</param>
        /// <returns>No content on success</returns>
        /// <remarks>
        /// Requires Admin role.
        /// This permanently deletes both the Doctor entity and the associated ApplicationUser from the database.
        /// This operation cannot be undone.
        /// The ID parameter refers to the UserId which is the primary key of the Doctor entity.
        /// </remarks>
        [HttpDelete("{id}/HardDelete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDeleteDoctor(string id)
        {
            try
            {
                await _doctorService.HardDeleteDoctorAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while hard deleting doctor with UserId: {UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while hard deleting doctor with UserId: {UserId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting doctor with UserId: {UserId}", id);
                return BadRequest(new { message = "An error occurred while hard deleting the doctor." });
            }
        }
    }
}

