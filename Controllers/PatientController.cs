using BreastCancer.DTO.request;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BreastCancer.Controllers
{
    [Route("api/patient")]
    [ApiController]
    [Authorize]
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
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10)</param>
        /// <returns>List of patients</returns>
        /// <remarks>SystemAdmin policy allows access only to users with role: Admin</remarks>
        [HttpGet]
        [Authorize(Policy = "SystemAdmin")]
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
        /// Get a patient by ID
        /// </summary>
        /// <param name="id">Patient ID</param>
        /// <returns>Patient details</returns>
        /// <remarks>ContentAccess policy allows access to users with roles: Doctor, Admin, Patient, or Caregiver</remarks>
        [HttpGet("{id}")]
        [Authorize(Policy = "ContentAccess")]
        public async Task<IActionResult> GetPatientById(string id)
        {
            try
            {
                var patient = await _patientService.GetPatientByIdAsync(id);
                if (patient == null)
                {
                    return NotFound(new { message = $"Patient with ID '{id}' not found." });
                }

                return Ok(patient);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while retrieving patient with ID: {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient with ID: {PatientId}", id);
                return BadRequest(new { message = "An error occurred while retrieving the patient." });
            }
        }

        /// <summary>
        /// Create a new patient
        /// </summary>
        /// <param name="patientDto">Patient creation data</param>
        /// <returns>Created patient</returns>
        /// <remarks>SystemAdmin policy allows access only to users with role: Admin</remarks>
        [HttpPost]
        [Authorize(Policy = "SystemAdmin")]
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
        /// <param name="id">Patient ID</param>
        /// <param name="patientDto">Patient update data</param>
        /// <returns>Updated patient</returns>
        /// <remarks>ContentAccess policy allows access to users with roles: Doctor, Admin, Patient, or Caregiver</remarks>
        [HttpPut("{id}")]
        [Authorize(Policy = "ContentAccess")]
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
                    return NotFound(new { message = $"Patient with ID '{id}' not found." });
                }

                return Ok(updatedPatient);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while updating patient with ID: {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating patient with ID: {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient with ID: {PatientId}", id);
                return BadRequest(new { message = "An error occurred while updating the patient." });
            }
        }

        /// <summary>
        /// Delete a patient (soft delete)
        /// </summary>
        /// <param name="id">Patient ID</param>
        /// <returns>No content on success</returns>
        /// <remarks>SystemAdmin policy allows access only to users with role: Admin</remarks>
        [HttpDelete("{id}")]
        [Authorize(Policy = "SystemAdmin")]
        public async Task<IActionResult> DeletePatient(string id, [FromQuery] bool hardDelete = false)
        {
            try
            {
                if (hardDelete)
                {
                    await _patientService.HardDeletePatientAsync(id);
                }
                else
                {
                    await _patientService.DeletePatientAsync(id);
                }
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while deleting patient with ID: {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while deleting patient with ID: {PatientId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient with ID: {PatientId}", id);
                return BadRequest(new { message = "An error occurred while deleting the patient." });
            }
        }
    }
}

