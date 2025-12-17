using BreastCancer.DTO.request;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BreastCancer.Controllers
{
    [Route("api/[controller]")]
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
        [HttpGet]
        [Authorize(Policy = "ContentAccess")]
        public async Task<IActionResult> GetAllPatients([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1)
                {
                    return BadRequest(new { message = "Page number must be greater than 0." });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new { message = "Page size must be between 1 and 100." });
                }

                var patients = await _patientService.GetAllPatientsAsync(pageNumber, pageSize);
                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all patients");
                return StatusCode(500, new { message = "An error occurred while retrieving patients." });
            }
        }

        /// <summary>
        /// Get a patient by ID
        /// </summary>
        /// <param name="id">Patient ID</param>
        /// <returns>Patient details</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = "ContentAccess")]
        public async Task<IActionResult> GetPatientById(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "Patient ID is required." });
                }

                var patient = await _patientService.GetPatientByIdAsync(id);
                if (patient == null)
                {
                    return NotFound(new { message = $"Patient with ID '{id}' not found." });
                }

                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient with ID: {PatientId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the patient." });
            }
        }

        /// <summary>
        /// Create a new patient
        /// </summary>
        /// <param name="patientDto">Patient creation data</param>
        /// <returns>Created patient</returns>
        [HttpPost]
        [Authorize(Policy = "MedicalAccess")]
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
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating patient");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                return StatusCode(500, new { message = "An error occurred while creating the patient." });
            }
        }

        /// <summary>
        /// Update an existing patient
        /// </summary>
        /// <param name="id">Patient ID</param>
        /// <param name="patientDto">Patient update data</param>
        /// <returns>Updated patient</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "MedicalAccess")]
        public async Task<IActionResult> UpdatePatient(string id, [FromBody] PatientUpdateDTO patientDto)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "Patient ID is required." });
                }

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
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating patient with ID: {PatientId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient with ID: {PatientId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the patient." });
            }
        }

        /// <summary>
        /// Delete a patient (soft delete)
        /// </summary>
        /// <param name="id">Patient ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "MedicalAccess")]
        public async Task<IActionResult> DeletePatient(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { message = "Patient ID is required." });
                }

                var deleted = await _patientService.DeletePatientAsync(id);
                if (!deleted)
                {
                    return NotFound(new { message = $"Patient with ID '{id}' not found." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient with ID: {PatientId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the patient." });
            }
        }
    }
}

