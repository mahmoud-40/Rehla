using BreastCancer.DTO.request;
using BreastCancer.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        [Authorize]
        public async Task<IActionResult> GetAllCaregivers()
        {
            var caregivers = await _caregiverService.GetAllCaregiversAsync();
            return Ok(caregivers);
        }

        [HttpPost]
        public IActionResult CreateCaregiver([FromBody] CaregiverCreateDTO caregiverDto)
        {
             _caregiverService.CreateCaregiver(caregiverDto);
            return CreatedAtAction(nameof(GetAllCaregivers), null);
        }
    }
}
