using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Service.Implementation;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BreastCancer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService accountService;
        public AccountController(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        /// <summary>
        /// Register a new doctor account
        /// </summary>
        /// <param name="doctor">Doctor registration data</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// Creates a new doctor account in the system. Requires valid registration data including personal information and professional details.
        /// </remarks>
        [HttpPost("Register/Doctor")]
        [SwaggerOperation(Summary = "Register a new doctor account")]
        [SwaggerResponse(StatusCodes.Status200OK, "Doctor registered successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid registration data or validation errors")]
        public async Task<IActionResult> RegisterDoctor(DoctorRegisterDTO doctor)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.DoctorRegister(doctor);
            if (result.IsSuccess)
                return Ok(new { message = "Doctor account created successfully. Please check your email to confirm your account." });

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
                break;
            }

            return BadRequest(ModelState);
        }

        /// <summary>
        /// Register a new patient account
        /// </summary>
        /// <param name="patient">Patient registration data</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// Creates a new patient account in the system. Requires valid registration data including personal information and optional medical history.
        /// </remarks>
        [HttpPost("Register/Patient")]
        [SwaggerOperation(Summary = "Register a new patient account")]
        [SwaggerResponse(StatusCodes.Status200OK, "Patient registered successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid registration data or validation errors")]
        public async Task<IActionResult> RegisterPatient(PatientRegisterDTO patient)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.PatientRegister(patient);
            if (result.IsSuccess)
                return Ok(new { message = "Patient account created successfully. Please check your email to confirm your account." });


            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
                break;
            }

            return BadRequest(ModelState);
        }

        /// <summary>
        /// Register a new caregiver account
        /// </summary>
        /// <param name="caregiver">Caregiver registration data</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// Creates a new caregiver account linked to a specific patient. Requires valid registration data and patient ID.
        /// </remarks>
        [HttpPost("Register/Caregiver")]
        [SwaggerOperation(Summary = "Register a new caregiver account")]
        [SwaggerResponse(StatusCodes.Status200OK, "Caregiver registered successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid registration data or validation errors")]
        public async Task<IActionResult> RegisterCaregiver(CaregiverRegisterDTO caregiver)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.CaregiverRegister(caregiver);
            if (result.IsSuccess)
                return Ok(new { message = "Caregiver account created successfully. Please check your email to confirm your account." });


            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
                break;
            }

            return BadRequest(ModelState);
        }

        /// <summary>
        /// Authenticate user and receive JWT tokens
        /// </summary>
        /// <param name="userFromRequest">Login credentials (email and password)</param>
        /// <returns>JWT access token, refresh token, and expiration time</returns>
        /// <remarks>
        /// Authenticates a user with their email and password. Returns JWT tokens for authorized API access.
        /// Use the access token in the Authorization header: "Bearer {accessToken}"
        /// </remarks>
        [HttpPost("Login")]
        [SwaggerOperation(Summary = "Authenticate user and receive JWT tokens")]
        [SwaggerResponse(StatusCodes.Status200OK, "Authentication successful, returns access token and refresh token")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid credentials or validation errors")]
        public async Task<IActionResult> Login(LoginDTO userFromRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.LoginAsync(userFromRequest);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Errors.First() });

            return Ok(new TokenResponseDTO
            { 
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresTime = result.ExpiresTime 
            });
        }

        /// <summary>
        /// Logout and invalidate refresh token
        /// </summary>
        /// <param name="logoutDTO">Refresh token to invalidate</param>
        /// <returns>Success message</returns>
        /// <remarks>
        /// Logs out the current user by invalidating the provided refresh token. Requires authentication.
        /// </remarks>
        [HttpPost("Logout")]
        [Authorize]
        [SwaggerOperation(Summary = "Logout and invalidate refresh token")]
        [SwaggerResponse(StatusCodes.Status200OK, "Logged out successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid refresh token")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized access")]
        public async Task<IActionResult> LogoutAsync(LogoutDTO logoutDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await accountService.LogoutAsync(logoutDTO);

            if (!success)
                return BadRequest(new { message = "Invalid refresh token"});

            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token DTO containing the refresh token</param>
        /// <returns>New JWT access token, refresh token, and expiration time</returns>
        /// <remarks>
        /// Generates a new access token and refresh token pair using a valid refresh token.
        /// Use this endpoint when the access token expires.
        /// </remarks>
        [HttpPost("RefreshToken")]
        [SwaggerOperation(Summary = "Refresh access token using refresh token")]
        [SwaggerResponse(StatusCodes.Status200OK, "Token refresh successful, returns new access token and refresh token")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request data")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid or expired refresh token")]
        public async Task<IActionResult> RefreshToken([FromBody]RefreshTokenDTO refreshToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.RefreshTokenAsync(refreshToken);

            if (!result.IsSuccess) return Unauthorized(new { error = result.Errors.First() });

            return Ok(new TokenResponseDTO
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresTime = result.ExpiresTime
            });
        }

        // ====================== PASSWORD ======================
        [HttpPost("reset-password")]
        [Authorize]
        public async Task<IActionResult> ResetPasswordAsync(ResetPasswordDTO resetPassword)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.ResetPasswordAsync(resetPassword);

            if (result.IsSuccess)
                return Ok(new { message = "Password updated successfully." });

            ModelState.AddModelError("", result.Errors.First());
            return BadRequest(ModelState);
        }

        [HttpPost("forget-password/code")]
        public async Task<IActionResult> SendForgetPasswordCodeAsync(string Email)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.SendForgetPasswordCodeAsync(Email);

            if (result.IsSuccess)
                return Ok(new { message = "A password reset code has been sent to your email." });

            ModelState.AddModelError("", result.Errors.First());
            return BadRequest(ModelState);
        }
        [HttpPost("forget-password/confirm")]
        public async Task<IActionResult> ForgetPasswordAsync(ForgetPasswordDTO forgetPassword)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.ForgetPasswordAsync(forgetPassword);

            if (result.IsSuccess)
                return Ok(new { message = "Password has been reset successfully." });

            ModelState.AddModelError("", result.Errors.First());
            return BadRequest(ModelState);
        }

        
    }
}
