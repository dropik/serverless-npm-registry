using Microsoft.AspNetCore.Mvc;
using NPMRegistry.Models;

namespace NPMRegistry.Controllers;

[ApiController]
[Route("/")]
public class RegistryController : ControllerBase
{
    [HttpGet]
    public ActionResult<RegistryResponse> GetRegistryData()
    {
        return Ok(new RegistryResponse()
        {
            db_name = "Serverless NPM Registry",
        });
    }
}
