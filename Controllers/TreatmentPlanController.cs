using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace BreastCancer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TreatmentPlanController : ControllerBase
    {
        private readonly ITreatmentPlanService _treatmentPlanService;
        private readonly ILogger<TreatmentPlanController> _logger;

        public TreatmentPlanController(
            ITreatmentPlanService treatmentPlanService,
            ILogger<TreatmentPlanController> logger)
        {
            _treatmentPlanService = treatmentPlanService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "Patient, Admin")]
        [SwaggerOperation(Summary = "Create a new treatment plan")]
        [SwaggerResponse(StatusCodes.Status201Created, "Treatment plan created successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid treatment plan data or validation errors")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden - User does not have required role")]
        public async Task<IActionResult> CreateTreatmentPlan([FromBody] TreatmentPlanCreateDTO treatmentPlanDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Get patient ID from JWT token claims
                var patientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(patientId))
                {
                    return Unauthorized(new { message = "Unable to identify patient from token." });
                }

                var createdPlan = await _treatmentPlanService.CreateTreatmentPlanAsync(patientId, treatmentPlanDto);
                return CreatedAtAction(
                    nameof(CreateTreatmentPlan),
                    new { id = createdPlan.Id },
                    createdPlan);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating treatment plan");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating treatment plan");
                return BadRequest(new { message = "An error occurred while creating the treatment plan." });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Patient, Admin")]
        [SwaggerOperation(Summary = "Update an existing treatment plan")]
        [SwaggerResponse(StatusCodes.Status200OK, "Treatment plan updated successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid treatment plan data or validation errors")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden - User does not have required role or does not own the treatment plan")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Treatment plan not found")]
        public async Task<IActionResult> UpdateTreatmentPlan(int id, [FromBody] TreatmentPlanUpdateDTO treatmentPlanDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Get patient ID from JWT token claims
                var patientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(patientId))
                {
                    return Unauthorized(new { message = "Unable to identify patient from token." });
                }

                var updatedPlan = await _treatmentPlanService.UpdateTreatmentPlanAsync(id, patientId, treatmentPlanDto);
                return Ok(updatedPlan);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating treatment plan");
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { message = ex.Message });
                }
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to update treatment plan");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating treatment plan");
                return BadRequest(new { message = "An error occurred while updating the treatment plan." });
            }
        }

        [HttpPost("medicines/{medicineId}/mark-taken")]
        [Authorize(Roles = "Patient, Admin")]
        [SwaggerOperation(Summary = "Mark a medicine as taken")]
        [SwaggerResponse(StatusCodes.Status200OK, "Medicine marked as taken successfully with updated NextAlert")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid operation (e.g., medicine has ended)")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Forbidden - User does not have required role or does not own the treatment plan")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Medicine not found")]
        public async Task<IActionResult> MarkMedicineAsTaken(int medicineId)
        {
            try
            {
                // Get patient ID from JWT token claims
                var patientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(patientId))
                {
                    return Unauthorized(new { message = "Unable to identify patient from token." });
                }

                var updatedMedicine = await _treatmentPlanService.MarkMedicineAsTakenAsync(medicineId, patientId);
                return Ok(updatedMedicine);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while marking medicine as taken");
                if (ex.Message.Contains("not found"))
                {
                    return NotFound(new { message = ex.Message });
                }
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to mark medicine as taken");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking medicine as taken");
                return BadRequest(new { message = "An error occurred while marking the medicine as taken." });
            }
        }
    }
}
