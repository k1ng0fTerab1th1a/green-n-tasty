using Microsoft.AspNetCore.Mvc;

namespace Restaurant.Api.Controllers;

[ApiController]
[Route("hello")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Hello World From Lambda");
    }
}
