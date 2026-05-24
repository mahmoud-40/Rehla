using BreastCancer.DTO.request;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BreastCancer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaregiverController : ControllerBase
    {
        private readonly ICaregiverService _caregiverService;

        public CaregiverController(ICaregiverService caregiverService)
        {
            _caregiverService = caregiverService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Doctor")]
        [SwaggerOperation(Summary = "Get all caregivers")]
        [SwaggerResponse(StatusCodes.Status200OK, "Returns the list of caregivers")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No caregivers found")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        public async Task<IActionResult> GetAllCaregivers()
        {
            var caregivers = await _caregiverService.GetAllCaregivers();
            if (caregivers == null || !caregivers.Any())
            {
                return NotFound("No caregivers found.");
            }
            return Ok(caregivers);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get caregiver by ID")]
        [SwaggerResponse(StatusCodes.Status200OK, "Returns the caregiver details")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Caregiver not found")]
        public async Task<IActionResult> GetCaregiverById(string id)
        {
            try
            {
                var caregiver = await _caregiverService.GetCaregiverById(id);
                return Ok(caregiver);
            }
            catch (Exception ex) when (ex.Message == "Caregiver not found.")
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("patient/{patientId}")]
        [SwaggerOperation(Summary = "Get caregivers by patient ID")]
        [SwaggerResponse(StatusCodes.Status200OK, "Returns the list of caregivers for the patient")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "No caregivers found for the specified patient")]
        public async Task<IActionResult> GetCaregiverByPatientId(string patientId)
        {
            var caregivers = await _caregiverService.GetCaregiverByPatientId(patientId);
            if (caregivers == null || !caregivers.Any())
            {
                return NotFound("No caregivers found for the specified patient.");
            }
            return Ok(caregivers);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Create a new caregiver")]
        [SwaggerResponse(StatusCodes.Status201Created, "Caregiver created successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid caregiver data")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        public async Task<IActionResult> CreateCaregiver([FromBody] CaregiverCreateDTO caregiverDto)
        {
            await _caregiverService.CreateCaregiver(caregiverDto);
            return CreatedAtAction(nameof(GetAllCaregivers), null);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Update caregiver details")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Caregiver updated successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid caregiver data")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        public async Task<IActionResult> UpdateCaregiver(string id, [FromBody] CaregiverUpdateDTO updateDto)
        {
            await _caregiverService.UpdateCaregiver(id, updateDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete caregiver (soft delete)")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Caregiver deleted successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        public async Task<IActionResult> DeleteCaregiver(string id)
        {
            await _caregiverService.DeleteCaregiver(id);
            return NoContent();
        }

        [HttpDelete("hard/{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Hard delete caregiver")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Caregiver hard deleted successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        public async Task<IActionResult> HardDeleteCaregiverById(string id)
        {
            await _caregiverService.HardDeleteCaregiverById(id);
            return NoContent();
        }
    }
}
