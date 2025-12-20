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
        [HttpPost("Register")]
        public async Task<IActionResult> Register(BaseRegisterDTO UserFromRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.RegisterAsync(UserFromRequest);

            if (result.IsSuccess)
                return Ok("Created");

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }

            return BadRequest(ModelState);
        }

        [HttpPost("Register/Doctor")]
        public async Task<IActionResult> RegisterDoctor(DoctorRegisterDTO doctor)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.DoctorRegister(doctor);
            if (result.IsSuccess)
                return Ok(new { Message = "Patient registered successfully" });

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
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
                return Ok(new { Message = "Patient registered successfully" });

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
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
                return Ok(new { Message = "Patient registered successfully" });

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }

            return BadRequest(ModelState);
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDTO userFromRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.LoginAsync(userFromRequest);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.Errors });

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
                return BadRequest(new { message = "Invalid refresh token" });

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody]RefreshTokenDTO refreshToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.RefreshTokenAsync(refreshToken);

            if (!result.IsSuccess) return Unauthorized(new { error = result.Errors });

            return Ok(new TokenResponseDTO
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                ExpiresTime = result.ExpiresTime
            });
        }

    }
}