using BreastCancer.DTO.request;
using BreastCancer.DTO.response;
using BreastCancer.Models;
using BreastCancer.Service.Implementation;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

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

        // ====================== REGISTER ======================
        [HttpPost("Register/Doctor")]
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
        [HttpPost("Register/Patient")]
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
        [HttpPost("Register/Caregiver")]
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

        // ====================== AUTH ======================
        [HttpPost("ConfirmEmail")]
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
        [HttpPost("ResendConfirmation")]
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

        [HttpPost("Login")]
        public async Task<IActionResult> LoginAsync(LoginDTO userFromRequest)
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

        [HttpPost("Logout")]
        [Authorize]
        public async Task<IActionResult> LogoutAsync(LogoutDTO logoutDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await accountService.LogoutAsync(logoutDTO);

            if (!success)
                return BadRequest(new { message = "Invalid refresh token"});

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshTokenAsync(RefreshTokenDTO refreshToken)
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