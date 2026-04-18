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
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get all users")]
        [SwaggerResponse(StatusCodes.Status200OK, "Returns the list of users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("users/role/{role}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Get users by role")]
        [SwaggerResponse(StatusCodes.Status200OK, "Returns the list of users for the role")]
        public async Task<IActionResult> GetUsersByRole(string role)
        {
            var users = await _adminService.GetUsersByRoleAsync(role);
            return Ok(users);
        }

        [HttpDelete("users")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete user")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "User deleted successfully")]
        public async Task<IActionResult> DeleteUser([FromBody] AdminDeleteUserDTO request)
        {
            await _adminService.DeleteUserAsync(request);
            return NoContent();
        }

        [HttpPost("users/disable")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Disable user account")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "User disabled successfully")]
        public async Task<IActionResult> DisableUser([FromBody] AdminDisableUserDTO request)
        {
            await _adminService.DisableUserAsync(request);
            return NoContent();
        }

        [HttpPost("assign/doctor-patient")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Assign doctor to patient")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Doctor assigned to patient")]
        public async Task<IActionResult> AssignDoctorToPatient([FromBody] AssignDoctorToPatientDTO request)
        {
            await _adminService.AssignDoctorToPatientAsync(request);
            return NoContent();
        }

        [HttpPost("assign/caregiver-patient")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Assign caregiver to patient")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Caregiver assigned to patient")]
        public async Task<IActionResult> AssignCaregiverToPatient([FromBody] AssignCaregiverToPatientDTO request)
        {
            await _adminService.AssignCaregiverToPatientAsync(request);
            return NoContent();
        }
    }
}

