using Microsoft.AspNetCore.Mvc;

namespace PatientManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok();
    }
}
