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

        // ============================== REGISTER ==============================
        /// <summary>
        /// Registers a new doctor account.
        /// </summary>
        /// <param name="doctor">Doctor registration data.</param>
        /// <remarks>
        /// Creates a new doctor account and sends an email confirmation code.
        /// 
        /// Password Rules:
        /// - Minimum 8 characters
        /// - At least one uppercase letter
        /// - At least one lowercase letter
        /// - At least one digit
        /// - At least one special character
        /// 
        /// The user cannot login until the email is confirmed.
        /// </remarks>
        [HttpPost("Register/Doctor")]
        [SwaggerOperation(
            Summary = "Register a new doctor account",
            Description = "Creates a doctor account and sends an email confirmation code."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Doctor registered successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation failed or email already exists")]       
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
        /// Registers a new patient account.
        /// </summary>
        /// <param name="patient">Patient registration data.</param>
        /// <remarks>
        /// Creates a new patient account and sends a confirmation email.
        /// Email confirmation is required before login.
        /// 
        /// Password Rules:
        /// Minimum 8 characters, uppercase, lowercase, digit, special character.
        /// </remarks>
        [HttpPost("Register/Patient")]
        [SwaggerOperation(
            Summary = "Register a new patient account",
            Description = "Creates patient account and sends email confirmation."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Patient registered successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation failed")]
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
        /// Registers a new caregiver account.
        /// </summary>
        /// <param name="caregiver">Caregiver registration data.</param>
        /// <remarks>
        /// Creates a caregiver account linked to a patient using PatientId.
        /// 
        /// Password Rules:
        /// Minimum 8 characters, uppercase, lowercase, digit, special character.
        /// </remarks>
        [HttpPost("Register/Caregiver")]
        [SwaggerOperation(
            Summary = "Register a new caregiver account",
            Description = "Creates caregiver account linked to a patient."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Caregiver registered successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid patient ID or validation failed")]
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

        // ============================== AUTH ==============================
        /// <summary>
        /// Confirms user's email address.
        /// </summary>
        /// <param name="confirmEmail">Email confirmation data.</param>
        /// <remarks>
        /// Confirms the user's email using the confirmation code sent after registration.
        /// The user cannot log in until the email is successfully confirmed.
        /// </remarks>
        [HttpPost("ConfirmEmail")]
        [SwaggerOperation(
            Summary = "Confirm email address",
            Description = "Validates confirmation code and activates the user account."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Email confirmed successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid or expired confirmation code")]
        public async Task<IActionResult> ConfirmEmailAsync(ConfirmEmailDTO confirmEmail)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.ConfirmEmailAsync(confirmEmail);

            if (result.IsSuccess)
                return Ok(new { message = "Email confirmed successfully. You can now log in." });

            ModelState.AddModelError("", result.Errors.First());
            return BadRequest(ModelState);

        }

        /// <summary>
        /// Resends email confirmation code.
        /// </summary>
        /// <param name="Email">Registered email address.</param>
        /// <remarks>
        /// Sends a new email confirmation code if the previous one expired or was not received.
        /// </remarks>
        [HttpPost("ResendConfirmation")]
        [SwaggerOperation(
            Summary = "Resend email confirmation code",
            Description = "Sends a new email confirmation code to the user's email."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Confirmation email sent")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Email not found or already confirmed")]
        public async Task<IActionResult> ResendConfirmationAsync(string Email)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.ResendConfirmationCodeAsync(Email);

            if (result.IsSuccess)
                return Ok(new { message = "Confirmation email has been sent. Please check your inbox." });

            ModelState.AddModelError("", result.Errors.First());
            return BadRequest(ModelState);

        }


        /// <summary>
        /// Authenticates user and returns JWT tokens.
        /// </summary>
        /// <param name="userFromRequest">Login credentials.</param>
        /// <remarks>
        /// User must confirm email before login.
        /// Returns AccessToken, RefreshToken, and expiration time.
        /// </remarks>
        [HttpPost("Login")]
        [SwaggerOperation(
            Summary = "Authenticate user",
            Description = "Returns JWT access token and refresh token."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Login successful")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid credentials")]

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
        /// Logs out current user.
        /// </summary>
        /// <param name="logoutDTO">Refresh token.</param>
        /// <remarks>
        /// Invalidates the refresh token.
        /// Requires authentication.
        /// </remarks>
        [Authorize]
        [HttpPost("Logout")]
        [SwaggerOperation(
            Summary = "Logout user",
            Description = "Invalidates refresh token."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Logout successful")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized")]

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
        /// Refreshes JWT access token.
        /// </summary>
        /// <param name="refreshToken">Refresh token data.</param>
        /// <remarks>
        /// Generates a new access token when the old one expires.
        /// </remarks>
        [HttpPost("RefreshToken")]
        [SwaggerOperation(
            Summary = "Refresh JWT token",
            Description = "Returns new access token and refresh token."
        )]
        [SwaggerResponse(StatusCodes.Status200OK, "Token refreshed successfully")]
        [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid refresh token")]

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
        /// <summary>
        /// Changes password for authenticated user.
        /// </summary>
        /// <param name="resetPassword">New password data.</param>
        /// <remarks>
        /// Requires authentication.
        /// Password rules apply.
        /// </remarks>
        [Authorize]
        [HttpPost("reset-password")]
        [SwaggerOperation(
            Summary = "Reset password",
            Description = "Allows authenticated user to change password."
        )]

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

        /// <summary>
        /// Sends password reset code to user's email.
        /// </summary>
        /// <param name="Email">Registered email address.</param>
        /// <remarks>
        /// Sends a one-time reset code to the user's email.
        /// </remarks>
        [HttpPost("forget-password/code")]
        [SwaggerOperation(
            Summary = "Send password reset code",
            Description = "Sends reset code to user's email."
        )]

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
        
        /// <summary>
        /// Resets password using confirmation code.
        /// </summary>
        /// <param name="forgetPassword">Reset password data.</param>
        /// <remarks>
        /// Validates reset code and updates the password.
        /// Password rules apply.
        /// </remarks>
        [HttpPost("forget-password/confirm")]
        [SwaggerOperation(
            Summary = "Confirm password reset",
            Description = "Validates reset code and updates password."
        )]
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
