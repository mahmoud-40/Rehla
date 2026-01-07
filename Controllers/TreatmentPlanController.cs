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

        /// <summary>
        /// Create a new treatment plan
        /// </summary>
        /// <param name="treatmentPlanDto">Treatment plan creation data including medicines</param>
        /// <returns>Created treatment plan with medicines</returns>
        /// <remarks>
        /// Creates a new treatment plan for the authenticated patient. 
        /// The patient ID is automatically extracted from the JWT token.
        /// Requires at least one medicine to be added to the plan.
        /// Doctor information can be provided either by DoctorId (if doctor exists in system) or DoctorName (for manual entry).
        /// Medicines will have their initial NextAlert set to StartTime. After the user marks a medicine as taken, 
        /// the NextAlert will be recalculated as LastTaken + IntervalHours.
        /// </remarks>
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

        /// <summary>
        /// Update an existing treatment plan
        /// </summary>
        /// <param name="id">Treatment plan ID</param>
        /// <param name="treatmentPlanDto">Treatment plan update data</param>
        /// <returns>Updated treatment plan with medicines</returns>
        /// <remarks>
        /// Updates an existing treatment plan. Only provided properties will be updated (null/empty values are ignored).
        /// Patients can only update their own treatment plans.
        /// 
        /// **Medicine Updates:**
        /// - Medicines with an `Id` will be updated (only non-null properties)
        /// - Medicines without an `Id` will be created as new medicines
        /// - Existing medicines not included in the `Medicines` array will be deleted
        /// 
        /// **Dynamic Medicine Scheduling:**
        /// - When updating a medicine's `LastTaken` property, the `NextAlert` will be automatically recalculated as `LastTaken + IntervalHours`
        /// - If `IntervalHours` is updated and `LastTaken` exists, `NextAlert` will be recalculated with the new interval
        /// - If the calculated `NextAlert` exceeds the medicine's `EndTime`, `NextAlert` will be set to null (no more alerts)
        /// 
        /// **Note:** This endpoint uses partial update - only send the fields you want to update.
        /// </remarks>
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
    }
}
