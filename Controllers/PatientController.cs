using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BreastCancer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize(Roles = "Patient")]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;
        private readonly ILogger<PatientController> _logger;

        public PatientController(IPatientService patientService, ILogger<PatientController> logger)
        {
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// Get all patients with pagination
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1, minimum: 1)</param>
        /// <param name="pageSize">Page size (default: 10, range: 1-100)</param>
        /// <returns>List of patients with pagination</returns>
        /// <remarks>
        /// Requires Admin role. Returns a paginated list of all patients in the system.
        /// </remarks>
        [HttpGet]
        // [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get all patients with pagination")]
        [SwaggerResponse(StatusCodes.Status200OK, "Returns the list of patients")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid pagination parameters or error occurred")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden - User does not have Admin role")]
        public async Task<IActionResult> GetAllPatients([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var patients = await _patientService.GetAllPatientsAsync(pageNumber, pageSize);
                return Ok(patients);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while retrieving all patients");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all patients");
                return BadRequest(new { message = "An error occurred while retrieving patients." });
            }
        }

        /// <summary>
        /// Get a patient by UserId
        /// </summary>
        /// <param name="id">Patient UserId (primary key)</param>
        /// <returns>Patient details</returns>
        /// <remarks>
        /// Requires one of the following roles: Doctor, Admin, Patient, or Caregiver.
        /// The ID parameter refers to the UserId which is the primary key of the Patient entity.
        /// </remarks>
        [HttpGet("{id}")]
        // [Authorize(Roles = "Doctor, Admin, Patient, Caregiver")]
        [SwaggerOperation(Summary = "Get a patient by UserId")]
        [SwaggerResponse(StatusCodes.Status200OK, "Returns the patient details")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request parameters or error occurred")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden - User does not have required role")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Patient not found")]
        public async Task<IActionResult> GetPatientById(string id)
        {
            try
            {
                var patient = await _patientService.GetPatientByIdAsync(id);
                if (patient == null)
                {
                    return NotFound(new { message = $"Patient with UserId '{id}' not found." });
                }

                return Ok(patient);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while retrieving patient with UserId: {UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient with UserId: {UserId}", id);
                return BadRequest(new { message = "An error occurred while retrieving the patient." });
            }
        }

        /// <summary>
        /// Create a new patient
        /// </summary>
        /// <param name="patientDto">Patient creation data</param>
        /// <returns>Created patient with generated UserId</returns>
        /// <remarks>
        /// Requires Admin role. Creates both an ApplicationUser and a Patient entity.
        /// A temporary password will be automatically generated for the patient.
        /// The returned ID is the UserId which serves as the Patient's primary key.
        /// </remarks>
        [HttpPost]
        // [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Create a new patient")]
        [SwaggerResponse(StatusCodes.Status201Created, "Patient created successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid patient data or validation errors")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden - User does not have Admin role")]
        public async Task<IActionResult> CreatePatient([FromBody] PatientCreateDTO patientDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdPatient = await _patientService.CreatePatientAsync(patientDto);
                return CreatedAtAction(
                    nameof(GetPatientById),
                    new { id = createdPatient.Id },
                    createdPatient);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while creating patient");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating patient");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                return BadRequest(new { message = "An error occurred while creating the patient." });
            }
        }

        /// <summary>
        /// Update an existing patient
        /// </summary>
        /// <param name="id">Patient UserId (primary key)</param>
        /// <param name="patientDto">Patient update data</param>
        /// <returns>Updated patient</returns>
        /// <remarks>
        /// Requires Admin or Patient role. Patients can only update their own information.
        /// The ID parameter refers to the UserId which is the primary key of the Patient entity.
        /// </remarks>
        [HttpPut("{id}")]
        // [Authorize(Roles = "Admin, Patient")]
        [SwaggerOperation(Summary = "Update an existing patient")]
        [SwaggerResponse(StatusCodes.Status200OK, "Patient updated successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid patient data or validation errors")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden - User does not have required role")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Patient not found")]
        public async Task<IActionResult> UpdatePatient(string id, [FromBody] PatientUpdateDTO patientDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedPatient = await _patientService.UpdatePatientAsync(id, patientDto);
                if (updatedPatient == null)
                {
                    return NotFound(new { message = $"Patient with UserId '{id}' not found." });
                }

                return Ok(updatedPatient);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while updating patient with UserId: {UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating patient with UserId: {UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient with UserId: {UserId}", id);
                return BadRequest(new { message = "An error occurred while updating the patient." });
            }
        }

        /// <summary>
        /// Delete a patient (soft delete - sets IsActive to false)
        /// </summary>
        /// <param name="id">Patient UserId (primary key)</param>
        /// <returns>No content on success</returns>
        /// <remarks>
        /// Requires Admin or Patient role. Patients can only delete their own account.
        /// This is a soft delete operation that sets the IsActive flag to false.
        /// The ID parameter refers to the UserId which is the primary key of the Patient entity.
        /// </remarks>
        [HttpDelete("{id}")]
        // [Authorize(Roles = "Admin, Patient")]
        [SwaggerOperation(Summary = "Delete a patient (soft delete - sets IsActive to false)")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Patient deleted successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request parameters or error occurred")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden - User does not have required role")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Patient not found")]
        public async Task<IActionResult> DeletePatient(string id)
        {
            try
            {
                await _patientService.DeletePatientAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while deleting patient with UserId: {UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while deleting patient with UserId: {UserId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient with UserId: {UserId}", id);
                return BadRequest(new { message = "An error occurred while deleting the patient." });
            }
        }

        /// <summary>
        /// Hard delete a patient (permanently remove from database)
        /// </summary>
        /// <param name="id">Patient UserId (primary key)</param>
        /// <returns>No content on success</returns>
        /// <remarks>
        /// Requires Admin role. This permanently deletes both the Patient entity and the associated ApplicationUser from the database.
        /// This operation cannot be undone. The ID parameter refers to the UserId which is the primary key of the Patient entity.
        /// </remarks>
        [HttpDelete("{id}/HardDelete")]
        // [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Hard delete a patient (permanently remove from database)")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Patient permanently deleted successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request parameters or error occurred")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden - User does not have Admin role")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Patient not found")]
        public async Task<IActionResult> HardDeletePatient(string id)
        {
            try
            {
                await _patientService.HardDeletePatientAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while hard deleting patient with UserId: {UserId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while hard deleting patient with UserId: {UserId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting patient with UserId: {UserId}", id);
                return BadRequest(new { message = "An error occurred while hard deleting the patient." });
            }
        }
    }
}
