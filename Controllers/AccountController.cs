using BreastCancer.DTO.request;
using BreastCancer.Models;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> Register(RegisterDTO UserFromRequest)
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

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDTO userFromRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await accountService.LoginAsync(userFromRequest);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorsMessage});

            return Ok(new { Token = result.Token });
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            return Ok(new
            {
                message = "Logged out successfully. Please delete the token on the client."
            });
        }
    }
}

